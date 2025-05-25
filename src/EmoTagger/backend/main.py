from fastapi import FastAPI, File, UploadFile, Depends, HTTPException, Form
from fastapi.middleware.cors import CORSMiddleware
import librosa
import numpy as np
from joblib import load
from pydantic import BaseModel
from sqlalchemy.orm import Session
import logging
import traceback
from database import SessionLocal
from models import Base, AIFeedback
from sqlalchemy import func

# Loglama ayarları
logging.basicConfig(
    level=logging.DEBUG,
    format='%(asctime)s - %(name)s - %(levelname)s - %(message)s'
)
logger = logging.getLogger(__name__)

app = FastAPI()

# --- CORS AYARLARI (app = FastAPI()'dan hemen sonra) ---
app.add_middleware(
    CORSMiddleware,
    allow_origins=["http://localhost:7211", "https://localhost:7211"],  # Sadece frontend portunu ekle!  # Tüm originlere izin ver
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)

# --- DB dependency ---
def get_db():
    try:
        db = SessionLocal()
        logger.debug("Database connection established")
        yield db
    except Exception as e:
        logger.error(f"Database connection error: {str(e)}")
        raise HTTPException(status_code=500, detail="Database connection failed")
    finally:
        db.close()
        logger.debug("Database connection closed")

# --- Feedback Model ---
class FeedbackRequest(BaseModel):
    musicId: int
    tag: str
    feedback: str
    userId: int = None

# --- Feedback Endpoint ---
@app.post("/save_ai_feedback")
async def save_ai_feedback(feedback: FeedbackRequest, db: Session = Depends(get_db)):
    try:
        logger.info(f"Received feedback request for musicId: {feedback.musicId}")
        ai_feedback = AIFeedback(
            music_id=feedback.musicId,
            ai_tag=feedback.tag,
            user_feedback=feedback.feedback,
            user_id=feedback.userId

        )
        db.add(ai_feedback)
        db.commit()
        db.refresh(ai_feedback)
        logger.info(f"Feedback saved successfully for musicId: {feedback.musicId}")
        return {"success": True}
    except Exception as e:
        logger.error(f"Error saving feedback: {str(e)}\n{traceback.format_exc()}")
        raise HTTPException(status_code=500, detail=f"Failed to save feedback: {str(e)}")

# --- Model Yükle ---
try:
    logger.info("Loading emotion model...")
    model, le = load('music_emotion_model.pkl')
    logger.info("Emotion model loaded successfully")
except Exception as e:
    logger.error(f"Error loading model: {str(e)}\n{traceback.format_exc()}")
    raise Exception("Failed to load emotion model")

# --- Prediction Endpoint ---
@app.post("/predict")
async def predict(
    file: UploadFile = File(...),
    musicId: int = Form(None),
    db: Session = Depends(get_db)
):
    try:
        logger.info(f"Received prediction request for file: {file.filename}")
        
        # Dosya kontrolü
        if not file.filename.endswith((".mp3", ".wav")):
            raise HTTPException(status_code=400, detail="Only MP3 and WAV files are allowed")
        
        # Ses dosyasını yükle
        try:
            audio, sr = librosa.load(file.file, sr=22050, duration=10)
            logger.debug(f"Audio loaded successfully: shape={audio.shape}, sr={sr}")
        except Exception as e:
            logger.error(f"Error loading audio file: {str(e)}")
            raise HTTPException(status_code=400, detail="Invalid audio file format")
        
        # Özellik çıkarma
        try:
            mfcc = np.mean(librosa.feature.mfcc(y=audio, sr=sr, n_mfcc=13).T, axis=0)
            tempo, _ = librosa.beat.beat_track(y=audio, sr=sr)
            energy = np.mean(librosa.feature.rms(y=audio))
            spectral_centroid = np.mean(librosa.feature.spectral_centroid(y=audio, sr=sr))
            spectral_bandwidth = np.mean(librosa.feature.spectral_bandwidth(y=audio, sr=sr))
            zero_crossing_rate = np.mean(librosa.feature.zero_crossing_rate(y=audio))
            
            features = np.concatenate([
                mfcc,
                [tempo, energy, spectral_centroid, spectral_bandwidth, zero_crossing_rate]
            ]).reshape(1, -1)
            logger.debug(f"Features extracted: shape={features.shape}")
        except Exception as e:
            logger.error(f"Error extracting features: {str(e)}")
            raise HTTPException(status_code=500, detail="Failed to extract audio features")
        
        # Tahmin
        try:
            # Ensemble model tahmini
            pred_proba = model.predict_proba(features)[0]
            pred_index = np.argmax(pred_proba)
            tag = le.inverse_transform([pred_index])[0]
            
            # En yüksek 3 olasılığı al
            top_3_idx = np.argsort(pred_proba)[-3:][::-1]
            top_3_probs = pred_proba[top_3_idx]
            top_3_tags = le.inverse_transform(top_3_idx)
            
            predictions = {
                tag: float(prob) for tag, prob in zip(top_3_tags, top_3_probs)
            }
            
            logger.info(f"Prediction successful: {predictions}")
        except Exception as e:
            logger.error(f"Error making prediction: {str(e)}")
            raise HTTPException(status_code=500, detail="Failed to make prediction")

        # --- AI etiketi değişirse oyları sıfırla ---
        music_id = musicId
        if music_id is None:
            try:
                import re
                filename = file.filename
                match = re.search(r'(\d+)', filename)
                if match:
                    music_id = int(match.group(1))
            except Exception as e:
                logger.warning(f"music_id extraction failed: {e}")

        feedbacks_deleted = False
        previous_tag = None
        if music_id:
            last_feedback = db.query(AIFeedback).filter(AIFeedback.music_id == music_id).order_by(AIFeedback.id.desc()).first()
            if last_feedback:
                previous_tag = last_feedback.ai_tag
            if last_feedback and last_feedback.ai_tag != tag:
                db.query(AIFeedback).filter(AIFeedback.music_id == music_id).delete()
                db.commit()
                logger.info(f"AI etiketi değişti, music_id={music_id} için tüm feedbackler silindi.")
                feedbacks_deleted = True

        # --- Kullanıcı feedbacklerine göre güven oranı ayarla ---
        yes_count = db.query(func.count(AIFeedback.id)).filter(
            AIFeedback.music_id == music_id,
            AIFeedback.user_feedback == 'yes'
        ).scalar() or 0
        no_count = db.query(func.count(AIFeedback.id)).filter(
            AIFeedback.music_id == music_id,
            AIFeedback.user_feedback == 'no'
        ).scalar() or 0
        total_count = yes_count + no_count
        if total_count > 0:
            feedback_confidence = yes_count / total_count
        else:
            feedback_confidence = 0.0

        # --- Kullanıcıların verdiği manuel tag'lere göre oranı hesapla ---
        tag_count = db.execute(
            'SELECT COUNT(*) FROM "MusicTags" WHERE "MusicId" = :music_id',
            {'music_id': music_id}
        ).scalar() or 0
        tag_match_count = db.execute(
            'SELECT COUNT(*) FROM "MusicTags" WHERE "MusicId" = :music_id AND "Tag" = :tag',
            {'music_id': music_id, 'tag': tag}
        ).scalar() or 0
        if tag_count > 0:
            tag_match_confidence = tag_match_count / tag_count
        else:
            tag_match_confidence = 1.0

        # --- Topluluk güvenini ortalama olarak hesapla ---
        community_confidence = (feedback_confidence + tag_match_confidence) / 2
        model_confidence = float(pred_proba[pred_index])
        final_confidence = model_confidence * community_confidence

        return {
            "predictions": predictions,
            "top_tag": tag,
            "top_confidence": final_confidence,
            "model_confidence": model_confidence,
            "feedback_confidence": feedback_confidence,
            "tag_match_confidence": tag_match_confidence,
            "community_confidence": community_confidence,
            "features_shape": features.shape,
            "audio_duration": len(audio)/sr,
            "previous_tag": previous_tag,
            "feedbacks_deleted": feedbacks_deleted
        }
    except HTTPException as he:
        raise he
    except Exception as e:
        logger.error(f"Unexpected error in prediction: {str(e)}\n{traceback.format_exc()}")
        raise HTTPException(status_code=500, detail=f"Unexpected error: {str(e)}")

# --- Feedback Stats Endpoint ---
@app.get("/get_ai_feedback_stats")
async def get_ai_feedback_stats(musicId: int, userId: int = None, db: Session = Depends(get_db)):
    try:
        logger.info(f"Getting feedback stats for musicId: {musicId}")
        
        # Get total counts
        yes_count = db.query(func.count(AIFeedback.id)).filter(
            AIFeedback.music_id == musicId,
            AIFeedback.user_feedback == 'yes'
        ).scalar() or 0
        
        no_count = db.query(func.count(AIFeedback.id)).filter(
            AIFeedback.music_id == musicId,
            AIFeedback.user_feedback == 'no'
        ).scalar() or 0
        
        total_count = yes_count + no_count
        
        # Get user's vote if userId is provided
        user_vote = None
        if userId:
            user_feedback = db.query(AIFeedback).filter(
                AIFeedback.music_id == musicId,
                AIFeedback.user_id == userId
            ).first()
            if user_feedback:
                user_vote = user_feedback.user_feedback

        # Agresif değişim için iki koşul:
        # 1. No oyları Yes oylarından 3 veya daha fazla fazlaysa
        # 2. VEYA No oyları toplam oyların %60'ından fazlaysa
        should_change_tag = False
        if total_count >= 3:  # En az 3 oy olmalı
            no_yes_diff = no_count - yes_count
            no_percentage = (no_count / total_count) * 100 if total_count > 0 else 0
            
            if no_yes_diff >= 3 or no_percentage >= 60:
                should_change_tag = True
                # Tüm feedbackleri sil
                db.query(AIFeedback).filter(AIFeedback.music_id == musicId).delete()
                db.commit()
                logger.info(f"Agresif değişim tetiklendi: No-Yes farkı={no_yes_diff}, No yüzdesi={no_percentage}%, music_id={musicId}")
        
        return {
            "success": True,
            "yesCount": yes_count,
            "noCount": no_count,
            "totalCount": total_count,
            "userVote": user_vote,
            "shouldChangeTag": should_change_tag,
            "noYesDiff": no_count - yes_count,  # Frontend'e farkı da gönder
            "noPercentage": (no_count / total_count) * 100 if total_count > 0 else 0  # Frontend'e yüzdeyi de gönder
        }
    except Exception as e:
        logger.error(f"Error getting feedback stats: {str(e)}\n{traceback.format_exc()}")
        raise HTTPException(status_code=500, detail=f"Failed to get feedback stats: {str(e)}")
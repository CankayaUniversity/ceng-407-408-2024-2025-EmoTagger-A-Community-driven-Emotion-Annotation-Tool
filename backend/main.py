from fastapi import FastAPI, File, UploadFile, Depends, HTTPException
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
async def predict(file: UploadFile = File(...)):
    try:
        logger.info(f"Received prediction request for file: {file.filename}")
        
        # Dosya kontrolü
        if not file.filename.endswith(('.mp3', '.wav')):
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
            features = np.concatenate([mfcc, [tempo, energy]]).reshape(1, -1)
            logger.debug(f"Features extracted: shape={features.shape}")
        except Exception as e:
            logger.error(f"Error extracting features: {str(e)}")
            raise HTTPException(status_code=500, detail="Failed to extract audio features")
        
        # Boyut kontrolü ve padding
        if features.shape[1] < 15:
            features = np.pad(features, ((0,0),(0,15-features.shape[1])), 'constant')
            logger.debug(f"Features padded to shape: {features.shape}")
        
        # Tahmin
        try:
            pred = model.predict(features)[0]
            conf = np.max(model.predict_proba(features))
            tag = le.inverse_transform([pred])[0]
            logger.info(f"Prediction successful: tag={tag}, confidence={conf}")
        except Exception as e:
            logger.error(f"Error making prediction: {str(e)}")
            raise HTTPException(status_code=500, detail="Failed to make prediction")
        
        return {
            "tag": tag,
            "confidence": float(conf),
            "features_shape": features.shape,
            "audio_duration": len(audio)/sr
        }
    except HTTPException as he:
        raise he
    except Exception as e:
        logger.error(f"Unexpected error in prediction: {str(e)}\n{traceback.format_exc()}")
        raise HTTPException(status_code=500, detail=f"Unexpected error: {str(e)}")
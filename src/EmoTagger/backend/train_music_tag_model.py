# -*- coding: utf-8 -*-

import psycopg2
import librosa
import numpy as np
from sklearn.linear_model import LogisticRegression
from sklearn.ensemble import RandomForestClassifier, VotingClassifier
from sklearn.svm import SVC
from sklearn.preprocessing import LabelEncoder
from joblib import dump
import os
import logging
import pickle

# Loglama ayarları
logging.basicConfig(level=logging.DEBUG)
logger = logging.getLogger(__name__)

try:
    # --- 1. Database connection ---
    logger.info("Veritabanına bağlanılıyor...")
    conn = psycopg2.connect(
        dbname="emotagger",
        user="postgres",
        password="ankara123",
        host="localhost",
        port="5432"
    )
    cur = conn.cursor()
    logger.info("Veritabanı bağlantısı başarılı!")

    # MusicId, Tag ve dosya adını birlikte çek
    logger.info("Müzik etiketleri çekiliyor...")
    cur.execute('''
        SELECT t."MusicId", t."Tag", m."filename"
        FROM public."MusicTags" t
        JOIN public.music m ON t."MusicId" = m.musicid
    ''')
    rows = cur.fetchall()
    logger.info(f"Toplam {len(rows)} müzik etiketi bulundu.")

    # --- 2. Prepare data and labels ---
    X = []
    y = []
    music_folder = "wwwroot/music"
    processed_files = 0
    error_files = 0

    for music_id, tag, filename in rows:
        filepath = os.path.join(music_folder, filename)
        if not os.path.exists(filepath):
            logger.warning(f"Dosya bulunamadı: {filepath}")
            error_files += 1
            continue

        try:
            logger.debug(f"İşleniyor: {filename}")
            audio, sr = librosa.load(filepath, sr=22050, duration=10)
            mfcc = np.mean(librosa.feature.mfcc(y=audio, sr=sr, n_mfcc=13).T, axis=0)
            tempo, _ = librosa.beat.beat_track(y=audio, sr=sr)
            energy = np.mean(librosa.feature.rms(y=audio))
            spectral_centroid = np.mean(librosa.feature.spectral_centroid(y=audio, sr=sr))
            spectral_bandwidth = np.mean(librosa.feature.spectral_bandwidth(y=audio, sr=sr))
            zero_crossing_rate = np.mean(librosa.feature.zero_crossing_rate(y=audio))
            
            features = np.concatenate([
                mfcc, 
                [tempo, energy, spectral_centroid, spectral_bandwidth, zero_crossing_rate]
            ])
            X.append(features)
            y.append(tag)
            processed_files += 1
            logger.debug(f"Başarıyla işlendi: {filename}")
        except Exception as e:
            logger.error(f"Hata ({filepath}): {str(e)}")
            error_files += 1

    logger.info(f"İşlenen dosya sayısı: {processed_files}")
    logger.info(f"Hata alınan dosya sayısı: {error_files}")

    X = np.array(X)
    y = np.array(y)

    if len(X) == 0:
        logger.error("Hiç geçerli ses özelliği çıkarılamadı! Ses dosyalarını ve veritabanını kontrol edin.")
        exit()

    # --- 3. Encode labels ---
    logger.info("Etiketler kodlanıyor...")
    le = LabelEncoder()
    y_encoded = le.fit_transform(y)
    logger.info(f"Etiketler: {le.classes_}")

    # --- 4. Create and train ensemble model ---
    logger.info("Ensemble model oluşturuluyor...")
    # Base models
    lr_model = LogisticRegression(max_iter=1000, random_state=42)
    rf_model = RandomForestClassifier(n_estimators=100, random_state=42)
    svm_model = SVC(probability=True, random_state=42)

    # Create voting classifier
    ensemble_model = VotingClassifier(
        estimators=[
            ('lr', lr_model),
            ('rf', rf_model),
            ('svm', svm_model)
        ],
        voting='soft'  # Use probability predictions
    )

    # Train ensemble model
    logger.info("Model eğitiliyor...")
    ensemble_model.fit(X, y_encoded)

    # RandomForest modelini ayrıca eğit
    rf_model.fit(X, y_encoded)

    logger.info("Model eğitimi tamamlandı!")

    # --- 5. Save model and label encoder ---
    logger.info("Model kaydediliyor...")
    with open('music_emotion_model.pkl', 'wb') as f:
        pickle.dump((ensemble_model, le), f)
    logger.info("Model başarıyla kaydedildi!")

    # Print model information
    logger.info("\nModel Bilgileri:")
    logger.info(f"Eğitim örneği sayısı: {len(X)}")
    logger.info(f"Özellik sayısı: {X.shape[1]}")
    logger.info(f"Sınıf sayısı: {len(le.classes_)}")
    logger.info(f"Sınıflar: {le.classes_}")

    # Print feature importance from Random Forest
    try:
        rf_importance = rf_model.feature_importances_
        feature_names = ['tempo', 'energy', 'danceability', 'valence', 'acousticness', 
                        'instrumentalness', 'liveness', 'speechiness', 'mode', 'key',
                        'spectral_centroid', 'spectral_bandwidth', 'zero_crossing_rate',
                        'mfcc_1', 'mfcc_2', 'mfcc_3', 'mfcc_4', 'mfcc_5']
        
        # Özellik önemlerini sırala
        feature_importance = sorted(zip(feature_names, rf_importance), 
                                  key=lambda x: x[1], reverse=True)
        
        logger.info("\nEn önemli 5 özellik:")
        for feature, importance in feature_importance[:5]:
            logger.info(f"{feature}: {importance:.4f}")
    except Exception as e:
        logger.error(f"Özellik önemleri alınırken hata oluştu: {str(e)}")

except Exception as e:
    logger.error(f"Beklenmeyen hata: {str(e)}")
    raise
finally:
    if 'cur' in locals():
        cur.close()
    if 'conn' in locals():
        conn.close()
    logger.info("Veritabanı bağlantısı kapatıldı.")
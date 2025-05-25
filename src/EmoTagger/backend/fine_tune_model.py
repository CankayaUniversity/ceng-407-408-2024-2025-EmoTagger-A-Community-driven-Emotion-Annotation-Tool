import os
import numpy as np
import pandas as pd
from sklearn.linear_model import LogisticRegression
from sklearn.preprocessing import LabelEncoder
from joblib import dump, load
import psycopg2
import librosa
from datetime import datetime, timedelta
from collections import Counter

class ModelFineTuner:
    def __init__(self):
        self.model_path = 'music_emotion_model.pkl'
        self.feedback_threshold = 3  # Daha hızlı değişim için eşik düşürüldü
        self.cv_weight = 0.7  # CV'lerden gelen etiketlerin ağırlığı
        self.conn = psycopg2.connect(
            dbname="emotagger",
            user="postgres",
            password="ankara123",
            host="localhost",
            port="5432"
        )

    def get_training_data(self):
        """Eğitim verilerini veritabanından çek"""
        cur = self.conn.cursor()
        
        # --- cv_tags sorgusu kaldırıldı ---
        cv_data = []  # Boş bırakıldı, sadece feedback kullanılacak
        
        # Kullanıcı geri bildirimlerini al (user_feedback dahil)
        cur.execute('''
            SELECT 
                af.music_id, 
                af.ai_tag,
                m.filename,
                (
                    SELECT "Tag" 
                    FROM "MusicTags" 
                    WHERE "MusicId" = af.music_id 
                    ORDER BY "Id" DESC 
                    LIMIT 1
                ) as correct_tag,
                COUNT(*) OVER (PARTITION BY af.music_id) as tag_count,
                af.user_feedback
            FROM ai_feedback af
            JOIN music m ON af.music_id = m.musicid
            WHERE af.created_at > NOW() - INTERVAL '30 days'
        ''')
        feedback_data = cur.fetchall()
        
        return cv_data, feedback_data

    def prepare_training_data(self, cv_data, feedback_data):
        """Eğitim verilerini hazırla"""
        X = []
        y = []
        weights = []
        music_folder = "wwwroot/music"

        # CV verilerini işle
        for music_id, filename, cv_tag, cv_confidence in cv_data:
            filepath = os.path.join(music_folder, filename)
            if not os.path.exists(filepath):
                continue

            features = self.extract_features(filepath)
            if features is not None:
                X.append(features)
                y.append(cv_tag)
                weights.append(cv_confidence * self.cv_weight)

        # Geri bildirim verilerini işle (user_feedback ile)
        for music_id, ai_tag, filename, correct_tag, tag_count, user_feedback in feedback_data:
            filepath = os.path.join(music_folder, filename)
            if not os.path.exists(filepath):
                continue

            features = self.extract_features(filepath)
            if features is not None and correct_tag:
                X.append(features)
                y.append(correct_tag)
                # NO feedback için ağırlığı artır (daha agresif)
                if user_feedback == 'no':
                    feedback_weight = min(tag_count / 1.5, 5.0)  # NO için daha agresif ağırlık
                else:
                    feedback_weight = min(tag_count / 5, 1.0)  # YES için normal ağırlık
                weights.append(feedback_weight)

        return np.array(X), np.array(y), np.array(weights)

    def get_negative_feedback(self):
        """Negatif geri bildirimleri ve doğru etiketleri veritabanından çek"""
        cur = self.conn.cursor()
        cur.execute('''
            SELECT 
                af.music_id, 
                af.ai_tag,
                m.filename,
                (
                    SELECT "Tag" 
                    FROM "MusicTags" 
                    WHERE "MusicId" = af.music_id 
                    ORDER BY "Id" DESC 
                    LIMIT 1
                ) as correct_tag,
                COUNT(*) OVER (PARTITION BY af.music_id) as tag_count
            FROM ai_feedback af
            JOIN music m ON af.music_id = m.musicid
            WHERE af.user_feedback = 'no'
            AND af.created_at > NOW() - INTERVAL '30 days'
        ''')
        return cur.fetchall()

    def get_tag_statistics(self):
        """Etiket istatistiklerini getir"""
        cur = self.conn.cursor()
        cur.execute('''
            SELECT 
                tag,
                COUNT(*) as total_tags,
                SUM(CASE WHEN af.user_feedback = 'no' THEN 1 ELSE 0 END) as negative_feedback
            FROM "MusicTags" mt
            LEFT JOIN ai_feedback af ON mt."MusicId" = af.music_id
            WHERE mt.created_at > NOW() - INTERVAL '30 days'
            GROUP BY tag
        ''')
        return cur.fetchall()

    def extract_features(self, filepath):
        """Ses dosyasından özellik çıkar"""
        try:
            audio, sr = librosa.load(filepath, sr=22050, duration=10)
            mfcc = np.mean(librosa.feature.mfcc(y=audio, sr=sr, n_mfcc=13).T, axis=0)
            tempo, _ = librosa.beat.beat_track(y=audio, sr=sr)
            energy = np.mean(librosa.feature.rms(y=audio))
            return np.concatenate([mfcc, [tempo, energy]])
        except Exception as e:
            print(f"Özellik çıkarma hatası ({filepath}): {e}")
            return None

    def fine_tune_model(self):
        """Modeli geri bildirimlere ve CV verilerine göre ince ayar yap"""
        # Eğitim verilerini al
        cv_data, feedback_data = self.get_training_data()
        
        if len(feedback_data) < self.feedback_threshold and len(cv_data) < self.feedback_threshold:
            print(f"Yeterli eğitim verisi yok. Gereken: {self.feedback_threshold}")
            return False

        # Mevcut modeli yükle
        try:
            model, le = load(self.model_path)
        except Exception as e:
            print(f"Model yükleme hatası: {e}")
            return False

        # Eğitim verilerini hazırla
        X, y, weights = self.prepare_training_data(cv_data, feedback_data)
        
        if len(X) == 0:
            print("Eğitim verisi oluşturulamadı!")
            return False

        # Etiketleri dönüştür
        y_encoded = le.transform(y)

        # Modeli güncelle (ağırlıklı eğitim)
        model.fit(X, y_encoded, sample_weight=weights)

        # Güncellenmiş modeli kaydet
        dump((model, le), self.model_path)
        print("Model başarıyla güncellendi!")
        return True

    def adjust_confidence_threshold(self, tag):
        """Belirli bir etiket için güven eşiğini ayarla"""
        cur = self.conn.cursor()
        cur.execute('''
            SELECT 
                COUNT(*) as total,
                SUM(CASE WHEN user_feedback = 'no' THEN 1 ELSE 0 END) as negative,
                SUM(CASE WHEN user_feedback = 'yes' THEN 1 ELSE 0 END) as positive,
                (
                    SELECT COUNT(*) 
                    FROM "MusicTags" 
                    WHERE tag = %s 
                    AND created_at > NOW() - INTERVAL '30 days'
                ) as total_tags
            FROM ai_feedback
            WHERE ai_tag = %s
            AND created_at > NOW() - INTERVAL '30 days'
        ''', (tag, tag))
        result = cur.fetchone()
        
        if result and result[0] > 0:
            total = result[0]
            negative = result[1]
            positive = result[2]
            total_tags = result[3]
            
            # Negatif geri bildirim sayısı 5 veya daha fazlaysa
            if negative >= 5:
                # Eğer pozitif geri bildirimler negatiflerden fazlaysa etiketi tekrar aktif et
                if positive > negative:
                    # Pozitif oran arttıkça güven eşiği düşer
                    positive_ratio = positive / (positive + negative)
                    return 0.7 - (positive_ratio * 0.2)
                else:
                    # Hala çok fazla negatif geri bildirim varsa etiketi devre dışı bırak
                    return 1.0
            
            # Normal durumda negatif orana göre güven eşiğini ayarla
            negative_ratio = negative / total if total > 0 else 0
            
            # Negatif oran yüksekse güven eşiğini düşür
            if negative_ratio > 0.5:
                # Negatif oran arttıkça güven eşiği düşer (0.7'den 0.5'e kadar)
                return 0.7 - (negative_ratio * 0.2)
            # Negatif oran düşükse ve toplam etiket sayısı yüksekse güven eşiğini artır
            elif negative_ratio < 0.3 and total_tags > 20:
                return 0.7 + ((1 - negative_ratio) * 0.1)
            
        return 0.7  # Varsayılan güven eşiği

if __name__ == "__main__":
    tuner = ModelFineTuner()
    tuner.fine_tune_model() 
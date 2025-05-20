import librosa
import numpy as np
from sklearn.preprocessing import StandardScaler
import joblib
import os

class MusicAnalyzer:
    def __init__(self):
        self.model_path = os.path.join(os.path.dirname(__file__), 'models')
        self.scaler = StandardScaler()
        self.load_models()

    def load_models(self):
        """Eğitilmiş modelleri yükle"""
        try:
            self.mood_model = joblib.load(os.path.join(self.model_path, 'mood_model.pkl'))
            self.tempo_model = joblib.load(os.path.join(self.model_path, 'tempo_model.pkl'))
            self.energy_model = joblib.load(os.path.join(self.model_path, 'energy_model.pkl'))
            self.rhythm_model = joblib.load(os.path.join(self.model_path, 'rhythm_model.pkl'))
        except:
            print("Models not found. Please train the models first.")
            self.mood_model = None
            self.tempo_model = None
            self.energy_model = None
            self.rhythm_model = None

    def extract_features(self, audio_path):
        """Müzik dosyasından özellik çıkar"""
        try:
            # Ses dosyasını yükle
            y, sr = librosa.load(audio_path)

            # Temel özellikler
            tempo, _ = librosa.beat.beat_track(y=y, sr=sr)
            spectral_centroids = librosa.feature.spectral_centroid(y=y, sr=sr)[0]
            spectral_rolloff = librosa.feature.spectral_rolloff(y=y, sr=sr)[0]
            spectral_contrast = librosa.feature.spectral_contrast(y=y, sr=sr)[0]
            mfccs = librosa.feature.mfcc(y=y, sr=sr, n_mfcc=13)
            chroma = librosa.feature.chroma_stft(y=y, sr=sr)
            onset_env = librosa.onset.onset_strength(y=y, sr=sr)

            # Özellik vektörü oluştur
            features = {
                'tempo': tempo,
                'spectral_centroid_mean': np.mean(spectral_centroids),
                'spectral_centroid_std': np.std(spectral_centroids),
                'spectral_rolloff_mean': np.mean(spectral_rolloff),
                'spectral_rolloff_std': np.std(spectral_rolloff),
                'spectral_contrast_mean': np.mean(spectral_contrast),
                'spectral_contrast_std': np.std(spectral_contrast),
                'mfcc_mean': np.mean(mfccs, axis=1),
                'mfcc_std': np.std(mfccs, axis=1),
                'chroma_mean': np.mean(chroma, axis=1),
                'chroma_std': np.std(chroma, axis=1),
                'onset_mean': np.mean(onset_env),
                'onset_std': np.std(onset_env)
            }

            return features

        except Exception as e:
            print(f"Error extracting features: {str(e)}")
            return None

    def analyze_music(self, audio_path):
        """Müzik dosyasını analiz et"""
        try:
            # Özellikleri çıkar
            features = self.extract_features(audio_path)
            if features is None:
                return None

            # Özellikleri düzenle
            feature_vector = np.concatenate([
                [features['tempo']],
                [features['spectral_centroid_mean']],
                [features['spectral_centroid_std']],
                [features['spectral_rolloff_mean']],
                [features['spectral_rolloff_std']],
                [features['spectral_contrast_mean']],
                [features['spectral_contrast_std']],
                features['mfcc_mean'],
                features['mfcc_std'],
                features['chroma_mean'],
                features['chroma_std'],
                [features['onset_mean']],
                [features['onset_std']]
            ])

            # Özellikleri normalize et
            feature_vector = self.scaler.fit_transform(feature_vector.reshape(1, -1))

            # Tahminleri yap
            results = {
                'mood': self.predict_mood(feature_vector),
                'tempo': self.predict_tempo(feature_vector),
                'energy': self.predict_energy(feature_vector),
                'rhythm': self.predict_rhythm(feature_vector)
            }

            return results

        except Exception as e:
            print(f"Error analyzing music: {str(e)}")
            return None

    def predict_mood(self, features):
        """Duygu durumunu tahmin et"""
        if self.mood_model is None:
            return "Model not trained"
        return self.mood_model.predict(features)[0]

    def predict_tempo(self, features):
        """Tempo kategorisini tahmin et"""
        if self.tempo_model is None:
            return "Model not trained"
        return self.tempo_model.predict(features)[0]

    def predict_energy(self, features):
        """Enerji seviyesini tahmin et"""
        if self.energy_model is None:
            return "Model not trained"
        return self.energy_model.predict(features)[0]

    def predict_rhythm(self, features):
        """Ritim özelliklerini tahmin et"""
        if self.rhythm_model is None:
            return "Model not trained"
        return self.rhythm_model.predict(features)[0]

    def train_models(self, training_data):
        """Modelleri eğit"""
        # Bu fonksiyon eğitim verileriyle modelleri eğitecek
        # Şimdilik boş bırakıyoruz, eğitim verileri hazır olduğunda implement edilecek
        pass 
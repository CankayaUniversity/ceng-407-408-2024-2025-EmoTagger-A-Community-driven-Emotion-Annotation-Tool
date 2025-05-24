import os
import numpy as np
from sklearn.model_selection import train_test_split
from sklearn.ensemble import RandomForestClassifier
from sklearn.preprocessing import StandardScaler
import joblib
from music_analyzer import MusicAnalyzer

class ModelTrainer:
    def __init__(self):
        self.model_path = os.path.join(os.path.dirname(__file__), 'models')
        self.analyzer = MusicAnalyzer()
        self.scaler = StandardScaler()
        
        # Model klasörünü oluştur
        if not os.path.exists(self.model_path):
            os.makedirs(self.model_path)

    def prepare_training_data(self, music_files, labels):
        """Eğitim verilerini hazırla"""
        features_list = []
        for music_file in music_files:
            features = self.analyzer.extract_features(music_file)
            if features is not None:
                features_list.append(features)

        # Özellikleri düzenle
        X = []
        for features in features_list:
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
            X.append(feature_vector)

        X = np.array(X)
        X = self.scaler.fit_transform(X)
        
        return X, labels

    def train_mood_model(self, X, mood_labels):
        """Duygu durumu modelini eğit"""
        X_train, X_test, y_train, y_test = train_test_split(X, mood_labels, test_size=0.2, random_state=42)
        
        model = RandomForestClassifier(n_estimators=100, random_state=42)
        model.fit(X_train, y_train)
        
        # Model performansını değerlendir
        train_score = model.score(X_train, y_train)
        test_score = model.score(X_test, y_test)
        print(f"Mood Model - Train Score: {train_score:.3f}, Test Score: {test_score:.3f}")
        
        # Modeli kaydet
        joblib.dump(model, os.path.join(self.model_path, 'mood_model.pkl'))
        return model

    def train_tempo_model(self, X, tempo_labels):
        """Tempo modelini eğit"""
        X_train, X_test, y_train, y_test = train_test_split(X, tempo_labels, test_size=0.2, random_state=42)
        
        model = RandomForestClassifier(n_estimators=100, random_state=42)
        model.fit(X_train, y_train)
        
        # Model performansını değerlendir
        train_score = model.score(X_train, y_train)
        test_score = model.score(X_test, y_test)
        print(f"Tempo Model - Train Score: {train_score:.3f}, Test Score: {test_score:.3f}")
        
        # Modeli kaydet
        joblib.dump(model, os.path.join(self.model_path, 'tempo_model.pkl'))
        return model

    def train_energy_model(self, X, energy_labels):
        """Enerji modelini eğit"""
        X_train, X_test, y_train, y_test = train_test_split(X, energy_labels, test_size=0.2, random_state=42)
        
        model = RandomForestClassifier(n_estimators=100, random_state=42)
        model.fit(X_train, y_train)
        
        # Model performansını değerlendir
        train_score = model.score(X_train, y_train)
        test_score = model.score(X_test, y_test)
        print(f"Energy Model - Train Score: {train_score:.3f}, Test Score: {test_score:.3f}")
        
        # Modeli kaydet
        joblib.dump(model, os.path.join(self.model_path, 'energy_model.pkl'))
        return model

    def train_rhythm_model(self, X, rhythm_labels):
        """Ritim modelini eğit"""
        X_train, X_test, y_train, y_test = train_test_split(X, rhythm_labels, test_size=0.2, random_state=42)
        
        model = RandomForestClassifier(n_estimators=100, random_state=42)
        model.fit(X_train, y_train)
        
        # Model performansını değerlendir
        train_score = model.score(X_train, y_train)
        test_score = model.score(X_test, y_test)
        print(f"Rhythm Model - Train Score: {train_score:.3f}, Test Score: {test_score:.3f}")
        
        # Modeli kaydet
        joblib.dump(model, os.path.join(self.model_path, 'rhythm_model.pkl'))
        return model

    def train_all_models(self, music_files, mood_labels, tempo_labels, energy_labels, rhythm_labels):
        """Tüm modelleri eğit"""
        # Eğitim verilerini hazırla
        X, _ = self.prepare_training_data(music_files, mood_labels)
        
        # Modelleri eğit
        self.train_mood_model(X, mood_labels)
        self.train_tempo_model(X, tempo_labels)
        self.train_energy_model(X, energy_labels)
        self.train_rhythm_model(X, rhythm_labels)

def main():
    # Örnek kullanım
    trainer = ModelTrainer()
    
    # Eğitim verilerini hazırla
    music_files = []  # Müzik dosyalarının yolları
    mood_labels = []  # Duygu durumu etiketleri
    tempo_labels = []  # Tempo etiketleri
    energy_labels = []  # Enerji etiketleri
    rhythm_labels = []  # Ritim etiketleri
    
    # Modelleri eğit
    trainer.train_all_models(music_files, mood_labels, tempo_labels, energy_labels, rhythm_labels)

if __name__ == "__main__":
    main() 
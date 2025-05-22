import os
import numpy as np
import tensorflow as tf
from tensorflow.keras.models import load_model
import librosa
import soundfile as sf
from pathlib import Path

class MusicAnalyzer:
    def __init__(self):
        self.model = None
        self.sample_rate = 22050
        self.duration = 3  # seconds
        self.hop_length = 512
        self.n_mels = 128
        self.n_fft = 2048
        self.model_path = os.path.join(os.path.dirname(__file__), 'models', 'musicnn_model.h5')
        self.load_model()

    def load_model(self):
        """Load the Musicnn model"""
        try:
            if not os.path.exists(self.model_path):
                print("Model not found. Please download the model first.")
                return
            
            self.model = load_model(self.model_path)
            print("Model loaded successfully")
        except Exception as e:
            print(f"Error loading model: {str(e)}")

    def preprocess_audio(self, audio_path):
        """Preprocess audio file for model input"""
        try:
            # Load audio file
            y, sr = librosa.load(audio_path, sr=self.sample_rate)
            
            # Extract mel spectrogram
            mel_spec = librosa.feature.melspectrogram(
                y=y,
                sr=sr,
                n_mels=self.n_mels,
                n_fft=self.n_fft,
                hop_length=self.hop_length
            )
            
            # Convert to log scale
            mel_spec_db = librosa.power_to_db(mel_spec, ref=np.max)
            
            # Normalize
            mel_spec_norm = (mel_spec_db - mel_spec_db.min()) / (mel_spec_db.max() - mel_spec_db.min())
            
            # Reshape for model input
            mel_spec_norm = np.expand_dims(mel_spec_norm, axis=-1)
            mel_spec_norm = np.expand_dims(mel_spec_norm, axis=0)
            
            return mel_spec_norm
        except Exception as e:
            print(f"Error preprocessing audio: {str(e)}")
            return None

    def predict_emotion(self, audio_path):
        """Predict emotion from audio file"""
        try:
            if self.model is None:
                return None, 0.0

            # Preprocess audio
            features = self.preprocess_audio(audio_path)
            if features is None:
                return None, 0.0

            # Make prediction
            predictions = self.model.predict(features)
            
            # Get predicted class and confidence
            predicted_class = np.argmax(predictions[0])
            confidence = predictions[0][predicted_class]

            # Map class to emotion tag
            emotion_map = {
                0: "sad",
                1: "happy",
                2: "nostalgic",
                3: "energetic",
                4: "relaxing",
                5: "romantic"
            }

            predicted_tag = emotion_map.get(predicted_class, "unknown")
            
            # Generate explanation
            explanation = self.generate_explanation(predicted_tag, confidence)

            return predicted_tag, confidence, explanation

        except Exception as e:
            print(f"Error predicting emotion: {str(e)}")
            return None, 0.0, ""

    def generate_explanation(self, tag, confidence):
        """Generate explanation for the prediction"""
        explanations = {
            "sad": "The song has melancholic melodies and slower tempo.",
            "happy": "The song features upbeat rhythms and positive energy.",
            "nostalgic": "The song evokes memories with its classic elements.",
            "energetic": "The song has high energy and dynamic beats.",
            "relaxing": "The song has calming melodies and peaceful atmosphere.",
            "romantic": "The song expresses emotional and romantic feelings."
        }
        
        base_explanation = explanations.get(tag, "The song has unique characteristics.")
        confidence_text = "high" if confidence > 0.7 else "moderate" if confidence > 0.4 else "low"
        
        return f"{base_explanation} (Confidence: {confidence_text})" 
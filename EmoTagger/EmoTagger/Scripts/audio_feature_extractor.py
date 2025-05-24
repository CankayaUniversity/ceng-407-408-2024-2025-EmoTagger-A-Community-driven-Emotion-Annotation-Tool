# Scripts/audio_feature_extractor.py

import librosa
import numpy as np

def extract_features(file_path):
    try:
        y, sr = librosa.load(file_path, duration=30)
        mfcc = librosa.feature.mfcc(y=y, sr=sr, n_mfcc=40)
        return np.mean(mfcc.T, axis=0)
    except Exception as e:
        print(f"[ERROR] extract_features failed for {file_path}: {e}")
        return None

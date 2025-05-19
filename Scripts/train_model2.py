import os
import pandas as pd
import numpy as np
from sklearn.preprocessing import LabelEncoder
from sklearn.model_selection import train_test_split
from tensorflow.keras.models import Sequential
from tensorflow.keras.layers import Dense, Dropout
from tensorflow.keras.utils import to_categorical
from joblib import dump
from audio_feature_extractor import extract_features

# === CONFIG ===
CSV_PATH = "Scripts/predicted_emotions.csv"
AUDIO_DIR = r"C:\Users\hamza\OneDrive\Desktop\EMOTAGGER DeepL\ceng-407-408-2024-2025-EmoTagger-A-Community-driven-Emotion-Annotation-Tool-development\wwwroot\music"

# === LOAD CSV from Model 1 ===
df = pd.read_csv(CSV_PATH)

# === BALANCE EMOTION CLASSES ===
# Keep only rows with valid predicted emotions (non-empty)
df = df[df["Predicted Emotion"].notnull()]

# Count and display before balancing
print("Before balancing:")
print(df["Predicted Emotion"].value_counts())

# Minimum number of samples per class to keep it
MIN_SAMPLES = 3
df = df.groupby("Predicted Emotion").filter(lambda x: len(x) >= MIN_SAMPLES)

# Balance: take equal number of samples per class
balanced_df = df.groupby("Predicted Emotion").apply(lambda x: x.sample(n=MIN_SAMPLES, random_state=42)).reset_index(drop=True)

print("\nAfter balancing:")
print(balanced_df["Predicted Emotion"].value_counts())

# === FEATURE EXTRACTION ===
features = []
labels = []

for _, row in balanced_df.iterrows():
    filename = row["Filename"]
    label = row["Predicted Emotion"]

    full_path = os.path.join(AUDIO_DIR, filename)
    if not full_path.endswith(".mp3") and not full_path.endswith(".m4a"):
        if os.path.exists(full_path + ".mp3"):
            full_path += ".mp3"
        elif os.path.exists(full_path + ".m4a"):
            full_path += ".m4a"
        else:
            print(f"🚫 File not found: {full_path}")
            continue

    print(f"🔍 Processing: {full_path}")
    feat = extract_features(full_path)
    if feat is not None:
        features.append(feat)
        labels.append(label)
    else:
        print(f"❌ Could not extract features from {full_path}")

# === PREPARE FOR TRAINING ===
X = np.array(features)
y = np.array(labels)

le = LabelEncoder()
y_encoded = le.fit_transform(y)
y_cat = to_categorical(y_encoded)

X_train, X_test, y_train, y_test = train_test_split(X, y_cat, test_size=0.2, random_state=42)

# === BUILD MODEL ===
model = Sequential([
    Dense(128, activation='relu', input_shape=(X.shape[1],)),
    Dropout(0.3),
    Dense(64, activation='relu'),
    Dropout(0.3),
    Dense(y_cat.shape[1], activation='softmax')
])
model.compile(loss='categorical_crossentropy', optimizer='adam', metrics=['accuracy'])

model.fit(X_train, y_train, epochs=30, batch_size=8, validation_data=(X_test, y_test), verbose=1)

# === SAVE MODEL & ENCODER ===
model.save("Scripts/emotion_model.h5")
dump(le, "Scripts/label_encoder.joblib")

print("\n✅ Model training complete. Files saved.")

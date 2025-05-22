# -*- coding: utf-8 -*-

import psycopg2
import librosa
import numpy as np
from sklearn.linear_model import LogisticRegression
from sklearn.preprocessing import LabelEncoder
from joblib import dump
import os
# --- 1. Database connection ---
conn = psycopg2.connect(
    dbname="emotagger",
    user="postgres",
    password="ankara123",
    host="localhost",
    port="5432"
)
cur = conn.cursor()
# MusicId, Tag ve dosya adını birlikte çek
cur.execute('''
    SELECT t."MusicId", t."Tag", m."filename"
    FROM public."MusicTags" t
    JOIN public.music m ON t."MusicId" = m.musicid
''')
rows = cur.fetchall()

# --- 2. Prepare data and labels ---
X = []
y = []
music_folder = "wwwroot/music"

for music_id, tag, filename in rows:
    filepath = os.path.join(music_folder, filename)
    if not os.path.exists(filepath):
        print("File not found:", filepath)
        continue

    try:
        audio, sr = librosa.load(filepath, sr=22050, duration=10)
        mfcc = np.mean(librosa.feature.mfcc(y=audio, sr=sr, n_mfcc=13).T, axis=0)
        tempo, _ = librosa.beat.beat_track(y=audio, sr=sr)
        energy = np.mean(librosa.feature.rms(y=audio))
        features = np.concatenate([mfcc, [tempo, energy]])
        X.append(features)
        y.append(tag)
    except Exception as e:
        print("Error ({}): {}".format(filepath, e))

X = np.array(X)
y = np.array(y)

if len(X) == 0:
    print("No valid audio features could be extracted! Check your audio files and database.")
    exit()

# --- 3. Encode labels ---
le = LabelEncoder()
y_encoded = le.fit_transform(y)

# --- 4. Train model ---
model = LogisticRegression(max_iter=200)
model.fit(X, y_encoded)

# --- 5. Save model and label encoder ---
dump((model, le), 'music_emotion_model.pkl')
print("Model saved successfully! Labels:", le.classes_)
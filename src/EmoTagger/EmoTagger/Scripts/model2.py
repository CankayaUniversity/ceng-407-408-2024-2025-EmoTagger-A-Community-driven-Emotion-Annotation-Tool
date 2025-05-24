import os
import pandas as pd
import numpy as np
from tensorflow.keras.models import load_model
from joblib import load
from audio_feature_extractor import extract_features

# === CONFIG ===
AUDIO_DIR = r"C:\Users\hamza\OneDrive\Desktop\EMOTAGGER DeepL\ceng-407-408-2024-2025-EmoTagger-A-Community-driven-Emotion-Annotation-Tool-development\wwwroot\music"
CSV_PATH = "Scripts/predicted_emotions.csv"
HTML_OUT = "Scripts/model2_predictions.html"

# === Load model and label encoder ===
model = load_model("Scripts/emotion_model.h5")
le = load("Scripts/label_encoder.joblib")

# === Load CSV and filter only tagged songs ===
df = pd.read_csv(CSV_PATH)
df = df[df["Predicted Emotion"] != "No Tags"]  # ✅ Filter out untagged songs

predictions = []

for _, row in df.iterrows():
    filename = row["Filename"]
    path = os.path.join(AUDIO_DIR, filename)

    if not os.path.exists(path):
        print(f"[❌] File does not exist: {path}")
        continue

    print(f"[🔍] Extracting: {path}")
    feat = extract_features(path)
    if feat is None:
        print(f"[⚠️] Feature extraction failed: {filename}")
        continue

    feat = feat.reshape(1, -1)
    pred = model.predict(feat, verbose=0)
    predicted_label = le.inverse_transform([np.argmax(pred)])[0]

    predictions.append({
        "Filename": filename,
        "Title": row["Title"],
        "Model 2 Predicted Emotion": predicted_label
    })

# === SAVE HTML ===
result_df = pd.DataFrame(predictions)

if not result_df.empty:
    result_df.to_html(HTML_OUT, index=False)
    print("[✅] model2_predictions.html saved.")
else:
    with open(HTML_OUT, "w", encoding="utf-8") as f:
        f.write("<p style='color:red;'>No predictions were generated (only tagged songs used).</p>")
    print("[⚠] model2_predictions.html created but no data to show.")

import pandas as pd
from bs4 import BeautifulSoup
from transformers import pipeline

# === CONFIGURATION ===
INPUT_HTML = "Scripts/model3_predictions.html"
OUTPUT_HTML = "Scripts/model3_lyrics_emotions.html"
EMOTION_LABELS = ["happy", "sad", "energetic", "romantic", "nostalgic"]

# === Load and Parse HTML ===
with open(INPUT_HTML, "r", encoding="utf-8") as f:
    soup = BeautifulSoup(f, "html.parser")

rows = soup.find_all("tr")[1:]  # skip header

data = []
for row in rows:
    cols = row.find_all("td")
    if len(cols) == 4:
        data.append({
            "Filename": cols[0].text.strip(),
            "Title": cols[1].text.strip(),
            "Lyrics": cols[2].text.strip()
        })

df = pd.DataFrame(data)

# === Load Translation and Classification Models ===
print("🔄 Loading translation model (Turkish → English)...")
translator = pipeline("translation", model="Helsinki-NLP/opus-mt-tr-en")

print("🧠 Loading emotion classifier (BART)...")
classifier = pipeline("zero-shot-classification", model="facebook/bart-large-mnli")

# === Prediction Function ===
def predict_emotion(text):
    try:
        # Truncate for efficiency
        short_text = text[:500]
        # Translate Turkish to English
        translated = translator(short_text)[0]["translation_text"]
        # Classify translated text
        result = classifier(translated, candidate_labels=EMOTION_LABELS)
        return result["labels"][0]  # Top emotion
    except Exception as e:
        print(f"[⚠️] Error during prediction: {e}")
        return "Unknown"

# === Apply Predictions ===
print("🎧 Predicting emotions...")
df["Predicted Emotion"] = df["Lyrics"].apply(predict_emotion)

# === Save Results ===
df.to_html(OUTPUT_HTML, index=False)
print(f"\n✅ Saved results to: {OUTPUT_HTML}")

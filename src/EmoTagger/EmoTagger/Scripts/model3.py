import os
import pandas as pd
import whisper
from transformers import pipeline

# === CONFIGURATION ===
CSV_PATH = "Scripts/predicted_emotions.csv"  # Must contain a 'Filename' column
AUDIO_DIR = r"C:\Users\hamza\OneDrive\Desktop\EMOTAGGER DeepL\ceng-407-408-2024-2025-EmoTagger-A-Community-driven-Emotion-Annotation-Tool-development\wwwroot\music"
HTML_OUT = "Scripts/model3_predictions.html"
TRANSCRIPT_CSV = "Scripts/transcriptions_only.csv"

# === Load CSV ===
df = pd.read_csv(CSV_PATH)
df["Filename"] = df["Filename"].astype(str).str.strip().str.lower()
df = df[df["Predicted Emotion"] != "No Tags"].head(5)

if "Filename" not in df.columns:
    raise ValueError("❌ CSV is missing 'Filename' column.")

print("\n📂 Available files in music/:")
print(os.listdir(AUDIO_DIR))

# === Load models ===
print("\n🔊 Loading Whisper (large)...")
whisper_model = whisper.load_model("large")

print("💡 Loading emotion classifier...")
emotion_classifier = pipeline(
    "text-classification",
    model="j-hartmann/emotion-english-distilroberta-base",
    top_k=1
)

# === Process ===
results = []
transcript_only = []

for _, row in df.iterrows():
    filename = row["Filename"]
    audio_path = os.path.join(AUDIO_DIR, filename)

    print(f"\n[🔍] Checking file: {audio_path}")
    if not os.path.exists(audio_path):
        print(f"❌ File not found: {filename}")
        continue

    try:
        print(f"🎧 Transcribing: {filename}")
        transcription = whisper_model.transcribe(audio_path)["text"]
        print(f"📝 Text: {transcription[:80]}...")

        transcript_only.append({
            "Filename": filename,
            "Title": row.get("Title", "Unknown"),
            "Transcribed Text": transcription
        })

        # Truncate to 1000 characters max (safe for most HuggingFace models)
        truncated_text = transcription[:1000]
        result = emotion_classifier(truncated_text)

        emotion = result[0]["label"] if isinstance(result, list) and "label" in result[0] else "Unknown"

        print(f"🔍 Detected Emotion: {emotion}")

        results.append({
            "Filename": filename,
            "Title": row.get("Title", "Unknown"),
            "Transcribed Text": transcription,
            "Model 3 Emotion": emotion
        })

    except Exception as e:
        print(f"[⚠️] Error processing {filename}: {str(e)}")

# === Save Final Results ===
if results:
    pd.DataFrame(results).to_html(HTML_OUT, index=False)
    print(f"\n✅ Emotion results saved to: {HTML_OUT}")
else:
    with open(HTML_OUT, "w", encoding="utf-8") as f:
        f.write("<p style='color:red;'>No Model 3 results generated.</p>")
    print("\n⚠ No emotion results generated.")

# === Save Transcripts Separately ===
if transcript_only:
    pd.DataFrame(transcript_only).to_csv(TRANSCRIPT_CSV, index=False)
    print(f"🗒️ Raw transcriptions saved to: {TRANSCRIPT_CSV}")
import os
import sys
import pandas as pd
from sqlalchemy import create_engine
from collections import Counter
from tabulate import tabulate

# === Fix encoding to avoid Unicode errors in Windows terminal ===
sys.stdout.reconfigure(encoding='utf-8')

# === Set base path (same folder as this script) ===
base_path = os.path.dirname(os.path.abspath(__file__))

# === Connect to PostgreSQL database ===
db_url = "postgresql+psycopg2://postgres:Ank1234@localhost:5432/emotagger"
engine = create_engine(db_url)
# === Fetch all music files, with or without tags ===
query = """
SELECT 
    m.musicid,
    m.title,
    m.artist,
    m.filename,
    mt."Tag"
FROM public.music m
LEFT JOIN public."MusicTags" mt ON m.musicid = mt."MusicId";
"""

df = pd.read_sql(query, engine)

# === Group tags (can be NaN) ===
grouped = df.groupby(["musicid", "title", "artist", "filename"])["Tag"].apply(list).reset_index()

results = []

for _, row in grouped.iterrows():
    tag_list = [t for t in row["Tag"] if pd.notna(t)]
    if tag_list:
        tag_counts = Counter(tag_list)
        most_common_tag, count = tag_counts.most_common(1)[0]
        predicted_emotion = most_common_tag
        tag_string = " ".join(tag_list)
    else:
        predicted_emotion = "No Tags"
        tag_string = "No Tags"

    results.append({
        "Title": row["title"] or "Unknown",
        "Artist": row["artist"] or "Unknown",
        "Filename": row["filename"],
        "Tags": tag_string,
        "Predicted Emotion": predicted_emotion
    })

# === Save to file ===
result_df = pd.DataFrame(results)
result_df.to_csv(os.path.join(base_path, "predicted_emotions.csv"), index=False)
result_df.to_html(os.path.join(base_path, "predicted_emotions.html"), index=False)




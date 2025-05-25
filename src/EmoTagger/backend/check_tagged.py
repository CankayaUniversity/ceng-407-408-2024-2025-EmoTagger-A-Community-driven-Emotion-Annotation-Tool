import psycopg2

conn = psycopg2.connect(
    dbname="emotagger",
    user="postgres",
    password="ankara123",
    host="localhost",
    port="5432"
)
cur = conn.cursor()

# Etiketlenmiş müziklerin listesi
cur.execute('''
    SELECT m."filename", array_agg(t."Tag") as tags
    FROM public.music m
    JOIN public."MusicTags" t ON m.musicid = t."MusicId"
    GROUP BY m."filename"
    ORDER BY m."filename"
''')
tagged_files = cur.fetchall()

print(f"Etiketlenmiş benzersiz müzik sayısı: {len(tagged_files)}")
print("\nEtiketlenmiş müzikler ve etiketleri:")
for filename, tags in tagged_files:
    print(f"\n{filename}:")
    for tag in tags:
        print(f"- {tag}")

cur.close()
conn.close() 
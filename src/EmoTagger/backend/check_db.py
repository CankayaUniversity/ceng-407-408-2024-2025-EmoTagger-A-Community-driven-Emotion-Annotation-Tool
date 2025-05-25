import psycopg2

conn = psycopg2.connect(
    dbname="emotagger",
    user="postgres",
    password="ankara123",
    host="localhost",
    port="5432"
)
cur = conn.cursor()

# Toplam etiket sayısı
cur.execute('SELECT COUNT(*) FROM public."MusicTags"')
total_tags = cur.fetchone()[0]
print(f'Toplam etiket sayısı: {total_tags}')

# Benzersiz müzik sayısı
cur.execute('SELECT COUNT(DISTINCT "MusicId") FROM public."MusicTags"')
unique_music = cur.fetchone()[0]
print(f'Toplam benzersiz müzik sayısı: {unique_music}')

# Her etiketin kaç kez kullanıldığı
cur.execute('SELECT "Tag", COUNT(*) FROM public."MusicTags" GROUP BY "Tag" ORDER BY COUNT(*) DESC')
tag_counts = cur.fetchall()
print('\nEtiket dağılımı:')
for tag, count in tag_counts:
    print(f'{tag}: {count}')

cur.close()
conn.close() 
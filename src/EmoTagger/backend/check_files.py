import os
import psycopg2

# Veritabanı bağlantısı
conn = psycopg2.connect(
    dbname="emotagger",
    user="postgres",
    password="ankara123",
    host="localhost",
    port="5432"
)
cur = conn.cursor()

# Veritabanındaki dosya listesi
cur.execute('SELECT DISTINCT m."filename" FROM public.music m')
db_files = set(row[0] for row in cur.fetchall())
print(f"Veritabanındaki benzersiz dosya sayısı: {len(db_files)}")

# Dosya sistemindeki dosya listesi
music_folder = "wwwroot/music"
fs_files = set(f for f in os.listdir(music_folder) if f.endswith('.mp3'))
print(f"Dosya sistemindeki MP3 dosya sayısı: {len(fs_files)}")

# Veritabanında olup dosya sisteminde olmayan dosyalar
missing_files = db_files - fs_files
if missing_files:
    print("\nVeritabanında olup dosya sisteminde olmayan dosyalar:")
    for f in sorted(missing_files):
        print(f"- {f}")

# Dosya sisteminde olup veritabanında olmayan dosyalar
extra_files = fs_files - db_files
if extra_files:
    print("\nDosya sisteminde olup veritabanında olmayan dosyalar:")
    for f in sorted(extra_files):
        print(f"- {f}")

cur.close()
conn.close() 
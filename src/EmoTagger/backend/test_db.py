import psycopg2
import os

try:
    # Get database connection details from environment variables
    db_url = os.getenv("DATABASE_URL", "postgresql://postgres:ankara123@localhost/emotagger")
    
    # Parse the database URL
    if db_url.startswith("postgres://"):
        db_url = db_url.replace("postgres://", "postgresql://", 1)
    
    conn = psycopg2.connect(db_url)
    print("Veritabanı bağlantısı başarılı!")
    conn.close()
except Exception as e:
    print(f"Bağlantı hatası: {str(e)}") 
# analyze_song.py
import sys
import musicnn.extractor as mne
import json
import requests
import os
import logging

# Logging ayarları
logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)

if len(sys.argv) < 2:
    error_msg = {'error': 'URL parametresi eksik'}
    print(json.dumps(error_msg))
    sys.exit(1)

audio_url = sys.argv[1]
local_path = 'temp_song.mp3'

# Dosyayı indir
try:
    logger.info(f"Dosya indiriliyor: {audio_url}")
    r = requests.get(audio_url)
    r.raise_for_status()  # HTTP hatalarını kontrol et
    with open(local_path, 'wb') as f:
        f.write(r.content)
    logger.info("Dosya başarıyla indirildi")
except requests.exceptions.RequestException as e:
    error_msg = {'error': f'Dosya indirme hatası: {str(e)}'}
    logger.error(error_msg['error'])
    print(json.dumps(error_msg))
    sys.exit(1)
except Exception as e:
    error_msg = {'error': f'Beklenmeyen hata: {str(e)}'}
    logger.error(error_msg['error'])
    print(json.dumps(error_msg))
    sys.exit(1)

# Analiz et
try:
    logger.info("Müzik analizi başlatılıyor...")
    tags = mne.extractor(local_path)
    moods = tags['moods']
    ai_tag = max(moods, key=moods.get)
    confidence = moods[ai_tag]
    
    # Emoji eşleştirmeleri
    emoji_map = {
        'happy': '😄',
        'sad': '😢',
        'energetic': '🔥',
        'relaxing': '🧘',
        'romantic': '❤️',
        'nostalgic': '🌧️'
    }
    
    result = {
        'aiTag': ai_tag,
        'confidence': confidence,
        'moods': moods,
        'emoji': emoji_map.get(ai_tag.lower(), '🎵')
    }
    logger.info(f"Analiz tamamlandı: {ai_tag} ({confidence})")
except Exception as e:
    error_msg = {'error': f'Analiz hatası: {str(e)}'}
    logger.error(error_msg['error'])
    print(json.dumps(error_msg))
    sys.exit(1)

# Dosyayı sil
try:
    os.remove(local_path)
    logger.info("Geçici dosya silindi")
except Exception as e:
    logger.warning(f"Geçici dosya silinemedi: {str(e)}")

print(json.dumps(result))
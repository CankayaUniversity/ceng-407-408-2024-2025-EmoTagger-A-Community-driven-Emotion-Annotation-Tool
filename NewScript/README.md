# Müzik Analiz Sistemi

Bu proje, müzik dosyalarını analiz ederek duygu durumu, tempo, enerji ve ritim özelliklerini tahmin eden bir sistem içerir.

## Kurulum

1. Gerekli Python paketlerini yükleyin:
```bash
pip install -r requirements.txt
```

2. `models` klasörünü oluşturun (otomatik olarak oluşturulacaktır).

## Kullanım

### Model Eğitimi

1. Eğitim verilerinizi hazırlayın:
   - Müzik dosyalarının yollarını içeren bir liste
   - Her müzik dosyası için duygu durumu, tempo, enerji ve ritim etiketleri

2. Modelleri eğitin:
```python
from train_models import ModelTrainer

trainer = ModelTrainer()
trainer.train_all_models(
    music_files=['path/to/music1.mp3', 'path/to/music2.mp3'],
    mood_labels=['happy', 'sad'],
    tempo_labels=['fast', 'slow'],
    energy_labels=['high', 'low'],
    rhythm_labels=['complex', 'simple']
)
```

### Müzik Analizi

1. Müzik analizcisini başlatın:
```python
from music_analyzer import MusicAnalyzer

analyzer = MusicAnalyzer()
```

2. Bir müzik dosyasını analiz edin:
```python
results = analyzer.analyze_music('path/to/music.mp3')
print(results)
```

## Özellikler

- Duygu durumu tahmini
- Tempo analizi
- Enerji seviyesi tahmini
- Ritim özellikleri analizi

## Gereksinimler

- Python 3.8+
- librosa
- numpy
- scikit-learn
- joblib 
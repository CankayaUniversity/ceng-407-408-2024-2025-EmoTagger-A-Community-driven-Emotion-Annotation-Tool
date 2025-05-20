// Chart.js CDN yüklenmiş olmalı!

async function showAIAnalysis(musicId, songTitle, songArtist) {
    // Modal başlıklarını güncelle
    document.getElementById('modalSongTitle').textContent = songTitle || '-';
    document.getElementById('modalSongArtist').textContent = songArtist || '';
    document.getElementById('aiLoadingIndicator').style.display = 'block';
    document.getElementById('aiResultsContainer').style.display = 'none';

    // Fetch AI analysis
    const res = await fetch(`/AI/AnalyzeMusic?musicId=${musicId}`);
    const data = await res.json();
    if (!data.success) {
        document.getElementById('aiLoadingIndicator').innerHTML = 'Analiz başarısız!';
        return;
    }

    // Örnek: Ritme göre analiz gösterilecek (diğerleri için sekme/alan ekleyebilirsin)
    const rhythm = data.aiResults.byRhythm;
    document.getElementById('dominantEmotion').textContent = rhythm.dominant;
    document.getElementById('confidenceValue').textContent = Math.round(rhythm.confidence * 100) + '%';
    document.getElementById('confidenceFill').style.width = (rhythm.confidence * 100) + '%';
    document.getElementById('dominantEmotionIcon').textContent = getEmotionIcon(rhythm.dominant);

    // Dummy müzik özellikleri (backend'den döndürmek istersen ekle)
    document.getElementById('tempoValue').textContent = 'Hızlı';
    document.getElementById('rhythmValue').textContent = 'Dinamik';
    document.getElementById('energyValue').textContent = '70%';
    document.getElementById('tonalityValue').textContent = 'Major';

    // Duygu dağılımı bar chart
    drawBarChart('emotionChart', rhythm.distribution);

    // Kullanıcı vs AI karşılaştırma (radar chart)
    drawComparisonChart('comparisonChart', data.userTags, rhythm.distribution);

    // Uyum oranı (örnek: en yüksek ortak duygu yüzdesi)
    const agreement = calcAgreement(data.userTags, rhythm.distribution);
    document.getElementById('agreementValue').textContent = agreement + '%';

    document.getElementById('aiLoadingIndicator').style.display = 'none';
    document.getElementById('aiResultsContainer').style.display = 'block';
}

function getEmotionIcon(emotion) {
    switch(emotion) {
        case 'Happy': return '😄';
        case 'Sad': return '😢';
        case 'Nostalgic': return '🌧️';
        case 'Energetic': return '🔥';
        case 'Relaxing': return '🧘';
        case 'Romantic': return '❤️';
        default: return '🎵';
    }
}

function drawBarChart(canvasId, data) {
    const ctx = document.getElementById(canvasId).getContext('2d');
    if (window[canvasId + '_chart']) window[canvasId + '_chart'].destroy();
    window[canvasId + '_chart'] = new Chart(ctx, {
        type: 'bar',
        data: {
            labels: Object.keys(data),
            datasets: [{
                label: 'Duygu Dağılımı',
                data: Object.values(data),
                backgroundColor: '#4f8cff'
            }]
        },
        options: { responsive: false, plugins: { legend: { display: false } } }
    });
}

function drawComparisonChart(canvasId, userTags, aiTags) {
    const ctx = document.getElementById(canvasId).getContext('2d');
    if (window[canvasId + '_chart']) window[canvasId + '_chart'].destroy();
    const labels = Object.keys({...userTags, ...aiTags});
    window[canvasId + '_chart'] = new Chart(ctx, {
        type: 'radar',
        data: {
            labels: labels,
            datasets: [
                {
                    label: 'Kullanıcı Etiketleri',
                    data: labels.map(k => userTags[k] || 0),
                    backgroundColor: 'rgba(255,99,132,0.2)',
                    borderColor: 'rgba(255,99,132,1)'
                },
                {
                    label: 'AI Tahmini',
                    data: labels.map(k => aiTags[k] || 0),
                    backgroundColor: 'rgba(54,162,235,0.2)',
                    borderColor: 'rgba(54,162,235,1)'
                }
            ]
        },
        options: { responsive: false }
    });
}

function calcAgreement(userTags, aiTags) {
    // En yüksek ortak duygu yüzdesi (örnek metrik)
    let max = 0;
    for (let key in userTags) {
        if (aiTags[key]) {
            max = Math.max(max, Math.min(userTags[key], aiTags[key]));
        }
    }
    return max;
}

// Spotify kategorisini dinamik ve hızlı göster
async function updateSpotifyCategory(songTitle, songArtist) {
    const box = document.getElementById('spotifyCategoryResult');
    if (!box) return;
    box.textContent = 'Yükleniyor...';

    // 3 saniye sonra hala güncellenmediyse otomatik olarak hata göster
    let timeout = setTimeout(() => {
        if (box.textContent === 'Yükleniyor...') {
            box.textContent = 'Spotify\'da bulunamadı';
        }
    }, 3000);

    try {
        const controller = new AbortController();
        const id = setTimeout(() => controller.abort(), 2500); // 2.5 saniyede fetch iptal
        const res = await fetch(`/SpotifyAnalysis/Analyze?songName=${encodeURIComponent(songTitle)}&artist=${encodeURIComponent(songArtist)}`, { signal: controller.signal });
        clearTimeout(id);
        const data = await res.json();
        clearTimeout(timeout);
        if (data.success) {
            box.textContent = data.category;
        } else {
            box.textContent = 'Spotify\'da bulunamadı';
        }
    } catch (e) {
        box.textContent = 'Spotify\'da bulunamadı';
    }
}

// AI Analizi için JavaScript kodu - En Son Hali
document.addEventListener('DOMContentLoaded', function() {
    console.log("DOM Yüklendi - AI Analiz başlatılıyor");

    // FontAwesome ikonlarını ekle (eğer sayfada yoksa)
    if (!document.querySelector('link[href*="fontawesome"]')) {
        const fontAwesome = document.createElement('link');
        fontAwesome.rel = 'stylesheet';
        fontAwesome.href = 'https://cdnjs.cloudflare.com/ajax/libs/font-awesome/5.15.4/css/all.min.css';
        document.head.appendChild(fontAwesome);
    }

    // Chart.js kütüphanesini ekle (eğer sayfada yoksa)
    if (!window.Chart && !document.querySelector('script[src*="chart.js"]')) {
        const chartScript = document.createElement('script');
        chartScript.src = 'https://cdnjs.cloudflare.com/ajax/libs/Chart.js/3.7.0/chart.min.js';
        document.head.appendChild(chartScript);
    }

    // Mevcut tüm AI düğmelerini temizle
    const allAIButtons = document.querySelectorAll('.ai-float-button, #showAIAnalysisBtn');
    allAIButtons.forEach(btn => {
        if (btn && btn.parentNode) {
            btn.parentNode.removeChild(btn);
        }
    });

    // Tüm player-header-controls'ları temizle
    const allControls = document.querySelectorAll('.player-header-controls');
    allControls.forEach(ctrl => {
        if (ctrl && ctrl.parentNode) {
            ctrl.parentNode.removeChild(ctrl);
        }
    });

    // "Şu anda çalıyor" bölümünü bul
    const container = document.querySelector('.rainbow-bg');
    if (container) {
        // Yeni kontroller div'i oluştur
        const controlsDiv = document.createElement('div');
        controlsDiv.className = 'player-header-controls';

        // AI düğmesi oluştur
        const aiButton = document.createElement('button');
        aiButton.id = 'showAIAnalysisBtn';
        aiButton.className = 'ai-float-button';
        aiButton.title = 'Yapay Zeka Analizi';
        aiButton.innerHTML = '<i class="fas fa-brain"></i>';

        // Düğmeyi ekle
        controlsDiv.appendChild(aiButton);
        container.appendChild(controlsDiv);

        console.log("AI analiz düğmesi eklendi");
    }

    // Global değişkenler - her yerden erişilebilir olması için
    window.aiModal = document.getElementById('aiAnalysisModal');
    window.aiModalContent = document.getElementById('aiModalContent');
    window.aiLoadingIndicator = document.getElementById('aiLoadingIndicator');
    window.aiResultsContainer = document.getElementById('aiResultsContainer');
    window.aiModalHeader = document.getElementById('aiModalHeader');
    window.closeAIModal = document.getElementById('closeAIModal');
    window.minimizeAIModal = document.getElementById('minimizeAIModal');

    console.log("AI modal elemanları: ", {
        modal: window.aiModal ? "bulundu" : "bulunamadı",
        content: window.aiModalContent ? "bulundu" : "bulunamadı",
        loading: window.aiLoadingIndicator ? "bulundu" : "bulunamadı",
        results: window.aiResultsContainer ? "bulundu" : "bulunamadı"
    });

    // Düğmeye tıklama olayı ekle
    const showAIBtn = document.getElementById('showAIAnalysisBtn');
    if (showAIBtn) {
        showAIBtn.addEventListener('click', function() {
            console.log("AI düğmesine tıklandı, modal açılıyor");
            openModal();
        });
    }

    // Modal kapatma butonu
    if (window.closeAIModal) {
        window.closeAIModal.addEventListener('click', function() {
            console.log("Kapatma düğmesine tıklandı");
            closeModal();
        });
    }

    // Modal küçültme butonu
    if (window.minimizeAIModal) {
        window.minimizeAIModal.addEventListener('click', function() {
            console.log("Küçültme düğmesine tıklandı");
            toggleMinimize();
        });
    }

    // Modal açma fonksiyonu
    function openModal() {
        if (window.aiModal) {
            window.aiModal.style.display = 'block';

            // Modal'ı sağ üste konumlandır
            if (window.aiModalContent) {
                window.aiModalContent.style.top = '60px';
                window.aiModalContent.style.right = '20px';
                window.aiModalContent.style.left = 'auto';
            }

            // Şarkı bilgilerini güncelle
            updateSongInfo();

            // Hızlı analizi başlat
            startAIAnalysis();

            // Spotify kategorisini güncelle
            updateSpotifyCategory(window.currentTrackTitle, window.currentTrackArtist);
        } else {
            console.error("aiModal bulunamadı!");
        }
    }

    // Modal kapatma fonksiyonu
    function closeModal() {
        if (window.aiModal) {
            window.aiModal.style.display = 'none';

            // Modal küçültülmüş ise normal duruma getir
            if (window.aiModalContent && window.aiModalContent.classList.contains('ai-modal-minimized')) {
                window.aiModalContent.classList.remove('ai-modal-minimized');
                if (window.minimizeAIModal) {
                    window.minimizeAIModal.textContent = '-';
                }
            }
        }
    }

    // Modal küçültme/büyütme fonksiyonu
    function toggleMinimize() {
        if (window.aiModalContent) {
            window.aiModalContent.classList.toggle('ai-modal-minimized');

            if (window.aiModalContent.classList.contains('ai-modal-minimized')) {
                window.minimizeAIModal.textContent = '+';
                window.minimizeAIModal.title = 'Genişlet';
            } else {
                window.minimizeAIModal.textContent = '-';
                window.minimizeAIModal.title = 'Küçült';
            }
        }
    }

    // Şarkı bilgilerini güncelle
    function updateSongInfo() {
        const songTitleEl = document.getElementById('modalSongTitle');
        const songArtistEl = document.getElementById('modalSongArtist');

        if (songTitleEl && window.currentTrackTitle) {
            songTitleEl.textContent = window.currentTrackTitle;
        }

        if (songArtistEl && window.currentTrackArtist) {
            songArtistEl.textContent = window.currentTrackArtist;
        }
    }

    // AI analizi başlat - SÜPER HIZLI ANALİZ
    function startAIAnalysis() {
        console.log("Analiz başlatılıyor");

        if (!window.aiLoadingIndicator || !window.aiResultsContainer) {
            console.error("Yükleme göstergesi veya sonuç konteyneri bulunamadı");
            return;
        }

        // Yükleme göstergesini göster, sonuçları gizle
        window.aiLoadingIndicator.style.display = 'block';
        window.aiResultsContainer.style.display = 'none';

        // İlerleme çubuğunu sıfırla
        const progressFill = document.getElementById('aiProgressFill');
        const progressText = document.getElementById('aiProgressText');

        if (progressFill) progressFill.style.width = '0%';
        if (progressText) progressText.textContent = '0%';

        // SÜPER HIZLI ANALİZ
        let progress = 0;
        const interval = setInterval(function() {
            // Daha hızlı ilerlemesi için büyük artışlar
            progress += 25;

            if (progressFill) progressFill.style.width = progress + '%';
            if (progressText) progressText.textContent = progress + '%';

            // Eğer analiz tamamlandıysa
            if (progress >= 100) {
                clearInterval(interval);

                // Kısa bir bekleme ile sonuçları göster - bu sorunu çözecek
                setTimeout(function() {
                    console.log("Analiz tamamlandı, sonuçlar gösteriliyor");

                    try {
                        // Grafikleri oluştur
                        createCharts();

                        // Sonuçları göster, yükleme göstergesini gizle
                        window.aiLoadingIndicator.style.display = 'none';
                        window.aiResultsContainer.style.display = 'block';
                    } catch (e) {
                        console.error("Analiz sonuçları gösterilirken hata:", e);
                    }
                }, 300); // 300ms bekleme süresi
            }
        }, 10); // Süper hızlı güncellemeler için çok kısa interval
    }

    // Grafikleri oluştur
    function createCharts() {
        console.log("Grafikler oluşturuluyor");

        // Önceden hazırlanmış sabit veriler
        const emotions = ['Sad', 'Happy', 'Nostalgic', 'Energetic', 'Relaxing', 'Romantic'];
        const aiPredictions = [10, 45, 15, 20, 5, 5]; // Sabit değerler
        const userPredictions = [5, 50, 10, 25, 5, 5]; // Sabit değerler
        const colors = [
            'rgba(106, 90, 205, 0.7)',  // Sad
            'rgba(255, 165, 0, 0.7)',   // Happy
            'rgba(148, 0, 211, 0.7)',   // Nostalgic
            'rgba(255, 20, 147, 0.7)',  // Energetic
            'rgba(64, 224, 208, 0.7)',  // Relaxing
            'rgba(255, 105, 180, 0.7)'  // Romantic
        ];

        try {
            // Duygu dağılımı grafiği
            const emotionCtx = document.getElementById('emotionChart');
            if (emotionCtx) {
                console.log("Duygu grafiği oluşturuluyor");

                // Eğer önceden bir grafik varsa, temizle
                if (window.emotionChart instanceof Chart) {
                    window.emotionChart.destroy();
                }

                // Yeni grafik oluştur
                window.emotionChart = new Chart(emotionCtx, {
                    type: 'bar',
                    data: {
                        labels: emotions,
                        datasets: [{
                            label: 'Duygu Tahmini',
                            data: aiPredictions,
                            backgroundColor: colors,
                            borderColor: colors.map(c => c.replace('0.7', '1')),
                            borderWidth: 1
                        }]
                    },
                    options: {
                        responsive: true,
                        maintainAspectRatio: false,
                        animation: { duration: 100 },
                        plugins: { legend: { display: false } }
                    }
                });
            } else {
                console.error("emotionChart canvas bulunamadı");
            }

            // Karşılaştırma grafiği
            const comparisonCtx = document.getElementById('comparisonChart');
            if (comparisonCtx) {
                console.log("Karşılaştırma grafiği oluşturuluyor");

                // Eğer önceden bir grafik varsa, temizle
                if (window.comparisonChart instanceof Chart) {
                    window.comparisonChart.destroy();
                }

                // Yeni grafik oluştur
                window.comparisonChart = new Chart(comparisonCtx, {
                    type: 'radar',
                    data: {
                        labels: emotions,
                        datasets: [{
                            label: 'AI Tahmini',
                            data: aiPredictions,
                            backgroundColor: 'rgba(74, 74, 215, 0.2)',
                            borderColor: 'rgba(74, 74, 215, 0.8)',
                            borderWidth: 2
                        }, {
                            label: 'Kullanıcı Etiketleri',
                            data: userPredictions,
                            backgroundColor: 'rgba(255, 99, 132, 0.2)',
                            borderColor: 'rgba(255, 99, 132, 0.8)',
                            borderWidth: 2
                        }]
                    },
                    options: {
                        responsive: true,
                        maintainAspectRatio: false,
                        animation: { duration: 100 }
                    }
                });
            } else {
                console.error("comparisonChart canvas bulunamadı");
                // Spotify kutusuna hata yaz
                const box = document.getElementById('spotifyCategoryResult');
                if (box) box.textContent = 'Spotify\'da bulunamadı (grafik hatası)';
            }

            // UI elementlerini güncelle
            updateUIElements();

        } catch (e) {
            console.error("Grafik oluşturma hatası:", e);

            // Hata olsa bile UI elemanlarını güncellemeye çalış
            try {
                updateUIElements();
            } catch (e2) {
                console.error("UI elemanları güncellenirken hata:", e2);
            }
        }
    }

    // UI elementlerini güncelle
    function updateUIElements() {
        console.log("UI elemanları güncelleniyor");

        try {
            // Duygu ikonu
            const iconEl = document.getElementById('dominantEmotionIcon');
            if (iconEl) iconEl.textContent = '😄';

            // Duygu adı
            const nameEl = document.getElementById('dominantEmotion');
            if (nameEl) nameEl.textContent = 'Happy';

            // Güven değeri
            const confidenceFill = document.getElementById('confidenceFill');
            const confidenceValue = document.getElementById('confidenceValue');

            if (confidenceFill) confidenceFill.style.width = '45%';
            if (confidenceValue) confidenceValue.textContent = '45%';

            // Özellik değerleri
            const tempoValue = document.getElementById('tempoValue');
            const rhythmValue = document.getElementById('rhythmValue');
            const energyValue = document.getElementById('energyValue');
            const tonalityValue = document.getElementById('tonalityValue');

            if (tempoValue) tempoValue.textContent = 'Hızlı';
            if (rhythmValue) rhythmValue.textContent = 'Dinamik';
            if (energyValue) energyValue.textContent = '70%';
            if (tonalityValue) tonalityValue.textContent = 'Major';

            // Uyum oranı
            const agreementValue = document.getElementById('agreementValue');
            if (agreementValue) agreementValue.textContent = '85%';
        } catch (e) {
            console.error("UI elemanları güncellenirken hata:", e);
        }
    }

    // Modal'ı sürüklemek için
    if (window.aiModalHeader && window.aiModalContent) {
        let isDragging = false;
        let currentX;
        let currentY;
        let initialX;
        let initialY;
        let xOffset = 0;
        let yOffset = 0;

        window.aiModalHeader.addEventListener('mousedown', dragStart);
        document.addEventListener('mouseup', dragEnd);
        document.addEventListener('mousemove', drag);

        function dragStart(e) {
            if (e.target === window.aiModalHeader || window.aiModalHeader.contains(e.target)) {
                isDragging = true;
                initialX = e.clientX - xOffset;
                initialY = e.clientY - yOffset;
            }
        }

        function dragEnd(e) {
            isDragging = false;
        }

        function drag(e) {
            if (isDragging) {
                e.preventDefault();

                currentX = e.clientX - initialX;
                currentY = e.clientY - initialY;
                xOffset = currentX;
                yOffset = currentY;

                // Modal'ın mevcut pozisyonu
                const rect = window.aiModalContent.getBoundingClientRect();

                // Ekranın boyutları
                const screenWidth = window.innerWidth;
                const screenHeight = window.innerHeight;

                // Modal'ı ekran içinde tutmak için sınırlar
                let leftPos = rect.left + e.movementX;
                let topPos = rect.top + e.movementY;

                leftPos = Math.max(0, Math.min(leftPos, screenWidth - rect.width));
                topPos = Math.max(0, Math.min(topPos, screenHeight - rect.height));

                window.aiModalContent.style.left = leftPos + 'px';
                window.aiModalContent.style.top = topPos + 'px';
            }
        }
    }

    // Modal dışına tıklandığında kapatma
    if (window.aiModal) {
        window.aiModal.addEventListener('click', function(e) {
            if (e.target === window.aiModal) {
                closeModal();
            }
        });
    }

    // Şarkı değişimlerini dinle
    document.addEventListener('songChanged', function(event) {
        const musicId = event.detail.musicId;
        if (musicId) {
            loadCurrentSongTag(musicId);
            loadTagStats(musicId);
            updatePlayCounts(musicId);
            // Şarkı değişir değişmez geçmişi yenile
            loadRecentlyPlayed();
        }
    });

    // Çalınan şarkıyı vurgulamak için fonksiyon
    function updatePlayingNowClass(musicId) {
        if (!musicId) return;

        console.log("Çalınan şarkıyı vurgulama güncelleniyor:", musicId);

        // Tüm 'playing-now' sınıflarını kaldır
        const allTracks = document.querySelectorAll('.track-item');
        allTracks.forEach(track => {
            track.classList.remove('playing-now');
        });

        // Çalan şarkıya 'playing-now' sınıfını ekle
        const currentTrack = document.querySelector(`.track-item[data-id="${musicId}"]`);
        if (currentTrack) {
            currentTrack.classList.add('playing-now');
            console.log("Çalınan şarkı vurgulandı:", currentTrack);
        }
    }

    // Sayfalar arası geçişte playlist durumunu korumak için
    window.addEventListener('load', function() {
        console.log("Sayfa yüklendi, global çalma listesi durumu kontrol ediliyor");

        // Ana pencereden şarkı listesini al
        if (window.parent && window.parent.tracks && window.parent.tracks.length > 0) {
            console.log("Ana pencereden şarkı listesi alındı, toplam:", window.parent.tracks.length);
            window.tracks = window.parent.tracks;
            window.currentIndex = window.parent.currentIndex || 0;
        }

        // Mevcut şarkı bilgisini güncelle
        if (window.currentMusicId) {
            console.log("Mevcut şarkı ID:", window.currentMusicId);
            updatePlayingNowClass(window.currentMusicId);
        }
    });

    // Pencere boyutlandırma olayını dinle
    window.addEventListener('resize', function() {
        if (window.aiModal && window.aiModal.style.display === 'block' && window.aiModalContent) {
            // Modal'ı ekran içinde tut
            const rect = window.aiModalContent.getBoundingClientRect();
            const screenWidth = window.innerWidth;
            const screenHeight = window.innerHeight;

            let leftPos = rect.left;
            let topPos = rect.top;

            if (leftPos + rect.width > screenWidth) {
                leftPos = screenWidth - rect.width;
            }

            if (topPos + rect.height > screenHeight) {
                topPos = screenHeight - rect.height;
            }

            window.aiModalContent.style.left = Math.max(0, leftPos) + 'px';
            window.aiModalContent.style.top = Math.max(0, topPos) + 'px';
        }
    });
});

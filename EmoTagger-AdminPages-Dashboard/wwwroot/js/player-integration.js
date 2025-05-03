document.addEventListener('DOMContentLoaded', function () {
    console.log("Player entegrasyonu başlatılıyor...");

    // Global müzik oynatıcısı - sayfalar arası geçişte kullanılır
    const globalPlayer = document.getElementById('musicPlayer');

    // Listen & Tag sayfasında mıyız?
    const isListenTagPage = window.location.pathname.includes('/Dashboard/ListenTag');

    // Eğer globalPlayer daha önce tanımlanmadıysa oluştur
    if (!globalPlayer && typeof Audio !== 'undefined') {
        const newPlayer = document.createElement('audio');
        newPlayer.id = 'musicPlayer';
        newPlayer.style.display = 'none';
        document.body.appendChild(newPlayer);
        console.log("Global müzik oynatıcısı oluşturuldu");
    }

    // Recently Played listesini güncellemek için AJAX çağrısı
    function updateRecentlyPlayed() {
        // Sadece Listen & Tag sayfasındaysak güncelle
        if (!isListenTagPage) return;

        fetch('/Dashboard/GetRecentlyPlayed')
            .then(response => {
                if (!response.ok) {
                    throw new Error('Sunucu hatası: ' + response.status);
                }
                return response.text();
            })
            .then(html => {
                // Recently Played bölümünü bul ve güncelle
                const recentlyPlayedContainer = document.querySelector('.activity-section .card:first-child .list-group');
                if (recentlyPlayedContainer) {
                    recentlyPlayedContainer.innerHTML = html;
                    console.log("Recently Played listesi güncellendi");
                } else {
                    console.warn("Recently Played konteyneri bulunamadı");
                }
            })
            .catch(error => {
                console.error("Recently Played güncelleme hatası:", error);
            });
    }

    // globalPlayer için genel bir log fonksiyonu
    function logPlayedTrack(musicId) {
        if (!musicId) {
            console.warn("Log için geçerli bir müzik ID'si bulunamadı");
            return;
        }

        fetch('/Dashboard/LogPlayed', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
                MusicId: parseInt(musicId)
            })
        })
            .then(response => {
                if (!response.ok) {
                    throw new Error('Sunucu hatası: ' + response.status);
                }
                return response.json();
            })
            .then(data => {
                console.log("🎧 Dinlenme kaydedildi:", musicId);
                // Başarıyla loglandıktan sonra güncellemeleri yap
                updateRecentlyPlayed();
            })
            .catch(error => {
                console.error("Dinleme logu hatası:", error);
            });
    }

    // NowPlaying bileşenindeki müzik çalma olayını dinle
    if (globalPlayer) {
        let hasLogged = false;
        let lastLoggedMusicId = null;

        // Müzik çalmaya başladığında
        globalPlayer.addEventListener('play', function () {
            console.log("Müzik çalmaya başladı");
            hasLogged = false; // Yeni çalma başladığında log durumunu sıfırla
        });

        // Müzik çalarken zaman ilerledikçe
        globalPlayer.addEventListener('timeupdate', function () {
            // Müzik 1 saniyeden fazla çaldıysa ve henüz loglanmadıysa
            if (!hasLogged && this.currentTime > 1) {
                // trackData elementinden ID'yi bulmaya çalış
                const trackData = document.getElementById('trackData');
                if (trackData) {
                    const musicId = getCurrentTrackId();
                    if (musicId && musicId !== lastLoggedMusicId) {
                        logPlayedTrack(musicId);
                        hasLogged = true;
                        lastLoggedMusicId = musicId;
                    }
                } else {
                    // Listen & Tag sayfasında aktif slayttan ID'yi bul
                    const activeSlide = document.querySelector('.swiper-slide-active');
                    if (activeSlide) {
                        const musicId = activeSlide.getAttribute('data-music-id');
                        if (musicId && musicId !== lastLoggedMusicId) {
                            logPlayedTrack(musicId);
                            hasLogged = true;
                            lastLoggedMusicId = musicId;
                        }
                    }
                }
            }
        });

        // Çalan müziğin ID'sini al (NowPlaying bileşeninden)
        function getCurrentTrackId() {
            const trackData = document.getElementById('trackData');
            if (trackData) {
                const currentIndex = parseInt(trackData.dataset.currentIndex) || 0;
                const trackItems = trackData.querySelectorAll('.track-item');
                if (trackItems && trackItems.length > 0 && currentIndex < trackItems.length) {
                    return trackItems[currentIndex].dataset.id;
                }
            }
            return null;
        }
    }

    // Listen & Tag sayfasındaki albümlere tıklama işlevi
    if (isListenTagPage) {
        const albumCovers = document.querySelectorAll('.album-cover');
        albumCovers.forEach(cover => {
            cover.addEventListener('click', function (e) {
                e.preventDefault();

                // En yakın swiper slide'ı bul
                const slide = this.closest('.swiper-slide');
                if (slide) {
                    const musicId = slide.getAttribute('data-music-id');
                    const filename = slide.getAttribute('data-filename');
                    const title = slide.getAttribute('data-title');
                    const artist = slide.getAttribute('data-artist');

                    if (filename) {
                        // Global müzik çaları ayarla
                        if (globalPlayer) {
                            globalPlayer.src = `https://emomusicc.vercel.app/music/${encodeURIComponent(filename)}`;
                            globalPlayer.play()
                                .then(() => {
                                    console.log(`Müzik çalınıyor: ${title} - ${artist}`);
                                    // 1 saniye sonra otomatik olarak güncelleyeceğiz,
                                    // ama acil durumlarda burada da manuel güncellenebilir
                                    // updateRecentlyPlayed(); 
                                })
                                .catch(err => {
                                    console.error("Müzik çalma hatası:", err);
                                });
                        }
                    }
                }
            });
        });
    }

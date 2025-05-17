document.addEventListener('DOMContentLoaded', function () {
    console.log("Geliştirilmiş player entegrasyonu başlatılıyor...");

    // Global müzik oynatıcısı
    const audioPlayer = document.getElementById('musicPlayer');
    if (!audioPlayer) {
        console.error("Audio player bulunamadı!");
        return;
    }

    // Sayfa türünü kontrol et
    const isListenTagPage = window.location.pathname.includes('/Dashboard/ListenTag');
    const isListenMixedPage = window.location.pathname.includes('/Dashboard/ListenMixed');

    // Tüm müzik tıklama olaylarını dinle (delegasyon ile)
    document.addEventListener('click', function (e) {
        // Listen Mixed sayfasında müzik satırına tıklama
        if (e.target.closest('.music-row')) {
            const row = e.target.closest('.music-row');
            // Eğer zaten playMusic fonksiyonu bağlıysa müdahale etme
            if (row.getAttribute('onclick') && row.getAttribute('onclick').includes('playMusic')) {
                return; // Mevcut fonksiyon çalışacak
            }

            const musicId = row.getAttribute('data-music-id');
            if (musicId) {
                // Müzik ID'sini global değişkene kaydet
                window.currentMusicId = musicId;

                // Gerekli diğer bilgileri bul
                const titleCell = row.querySelector('td:nth-child(2)');
                const artistCell = row.querySelector('td:nth-child(3)');

                if (titleCell && artistCell) {
                    window.currentTrackTitle = titleCell.textContent.trim();
                    window.currentTrackArtist = artistCell.textContent.trim();
                }
            }
        }

        // Listen Tag sayfasında albüm kapağına tıklama
        if (isListenTagPage && e.target.closest('.album-cover')) {
            const slide = e.target.closest('.swiper-slide');
            if (slide) {
                const musicId = slide.getAttribute('data-music-id');
                const filename = slide.getAttribute('data-filename');
                const title = slide.getAttribute('data-title');
                const artist = slide.getAttribute('data-artist');

                if (filename && audioPlayer) {
                    // Çalma bilgilerini güncelle
                    window.currentMusicId = musicId;
                    window.currentTrackTitle = title;
                    window.currentTrackArtist = artist;

                    // Müziği çal
                    audioPlayer.src = `https://emomusicc.vercel.app/music/${encodeURIComponent(filename)}`;
                    audioPlayer.play().catch(err => console.error("Müzik çalma hatası:", err));

                    // UI güncelle
                    updateNowPlayingUI(title, artist);
                }
            }
        }
    });

    // Dinleme süresi takibi
    let hasLogged = false;

    audioPlayer.addEventListener('timeupdate', function () {
        // Şarkı 5 saniyeden fazla çalındıysa dinleme olarak kaydet
        if (!hasLogged && this.currentTime > 5 && window.currentMusicId) {
            logPlayedTrack(window.currentMusicId);
            hasLogged = true;
        }
    });

    audioPlayer.addEventListener('ended', function () {
        // Şarkı bittiğinde sıradaki şarkıyı çal
        if (typeof nextTrack === 'function') {
            nextTrack();
        }
        hasLogged = false; // Logging durumunu sıfırla
    });

    audioPlayer.addEventListener('play', function () {
        // Oynatma durumunu güncelle
        const playPauseIcon = document.querySelector('.play-pause-btn i');
        if (playPauseIcon) {
            playPauseIcon.className = 'fas fa-pause';
        }
    });

    audioPlayer.addEventListener('pause', function () {
        // Duraklama durumunu güncelle
        const playPauseIcon = document.querySelector('.play-pause-btn i');
        if (playPauseIcon) {
            playPauseIcon.className = 'fas fa-play';
        }
    });

    // Global oynatma kontrolü fonksiyonlarını yeniden tanımla
    window.playPause = function () {
        if (!audioPlayer) return;

        if (audioPlayer.paused) {
            audioPlayer.play().catch(e => console.error("Oynatma hatası:", e));
        } else {
            audioPlayer.pause();
        }
    };

    window.nextTrack = function () {
        // Track Data'dan bir sonraki şarkıyı çal
        const trackData = document.getElementById('trackData');
        if (trackData) {
            const currentIndex = parseInt(trackData.dataset.currentIndex) || 0;
            const trackCount = parseInt(trackData.dataset.tracksCount) || 0;

            if (trackCount > 0) {
                // Bir sonraki şarkı indeksini hesapla
                const nextIndex = (currentIndex + 1) % trackCount;

                // Tüm şarkıları al
                const trackItems = trackData.querySelectorAll('.track-item');
                if (trackItems && trackItems.length > nextIndex) {
                    const nextTrack = trackItems[nextIndex];

                    // Yeni şarkı bilgilerini al
                    const id = nextTrack.dataset.id;
                    const title = nextTrack.dataset.title;
                    const artist = nextTrack.dataset.artist;
                    const filename = nextTrack.dataset.filename;

                    // Şarkıyı ayarla ve çal
                    if (filename && audioPlayer) {
                        window.currentMusicId = id;
                        window.currentTrackTitle = title;
                        window.currentTrackArtist = artist;

                        audioPlayer.src = `https://emomusicc.vercel.app/music/${encodeURIComponent(filename)}`;
                        audioPlayer.play().catch(err => console.error("Müzik çalma hatası:", err));

                        // UI güncelle
                        updateNowPlayingUI(title, artist);
                        hasLogged = false; // Yeni şarkı için log durumunu sıfırla

                        // Current index'i güncelle
                        trackData.dataset.currentIndex = nextIndex;
                    }
                }
            }
        }
    };

    window.prevTrack = function () {
        // Track Data'dan bir önceki şarkıyı çal
        const trackData = document.getElementById('trackData');
        if (trackData) {
            const currentIndex = parseInt(trackData.dataset.currentIndex) || 0;
            const trackCount = parseInt(trackData.dataset.tracksCount) || 0;

            if (trackCount > 0) {
                // Bir önceki şarkı indeksini hesapla
                const prevIndex = (currentIndex - 1 + trackCount) % trackCount;

                // Tüm şarkıları al
                const trackItems = trackData.querySelectorAll('.track-item');
                if (trackItems && trackItems.length > prevIndex) {
                    const prevTrack = trackItems[prevIndex];

                    // Yeni şarkı bilgilerini al
                    const id = prevTrack.dataset.id;
                    const title = prevTrack.dataset.title;
                    const artist = prevTrack.dataset.artist;
                    const filename = prevTrack.dataset.filename;

                    // Şarkıyı ayarla ve çal
                    if (filename && audioPlayer) {
                        window.currentMusicId = id;
                        window.currentTrackTitle = title;
                        window.currentTrackArtist = artist;

                        audioPlayer.src = `https://emomusicc.vercel.app/music/${encodeURIComponent(filename)}`;
                        audioPlayer.play().catch(err => console.error("Müzik çalma hatası:", err));

                        // UI güncelle
                        updateNowPlayingUI(title, artist);
                        hasLogged = false; // Yeni şarkı için log durumunu sıfırla

                        // Current index'i güncelle
                        trackData.dataset.currentIndex = prevIndex;
                    }
                }
            }
        }
    };

    window.stopTrack = function () {
        if (!audioPlayer) return;

        audioPlayer.pause();
        audioPlayer.currentTime = 0;

        // UI güncelle
        const playPauseIcon = document.querySelector('.play-pause-btn i');
        if (playPauseIcon) {
            playPauseIcon.className = 'fas fa-play';
        }
    };

    // Yardımcı fonksiyonlar
    function logPlayedTrack(musicId) {
        if (!musicId) return;

        fetch('/Dashboard/LogPlayed', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
                MusicId: parseInt(musicId)
            })
        })
            .then(response => response.json())
            .then(data => {
                console.log("🎧 Dinleme kaydedildi:", musicId);
                // Dinleme geçmişini güncelle (eğer sayfadaysa)
                if (isListenTagPage) {
                    updateRecentlyPlayed();
                }
            })
            .catch(error => {
                console.error("Dinleme logu hatası:", error);
            });
    }

    function updateRecentlyPlayed() {
        fetch('/Dashboard/GetRecentlyPlayed')
            .then(response => response.text())
            .then(html => {
                const recentlyPlayedContainer = document.querySelector('.activity-section .card:first-child .list-group');
                if (recentlyPlayedContainer) {
                    recentlyPlayedContainer.innerHTML = html;
                }
            })
            .catch(err => {
                console.error("Recently Played güncelleme hatası:", err);
            });
    }

    function updateNowPlayingUI(title, artist) {
        // Başlık güncelleme
        const marqueeContent = document.querySelector('.marquee-content');
        if (marqueeContent && title && artist) {
            marqueeContent.innerHTML = `<strong>${title} - ${artist}</strong>`;

            // Sayfa başlığını güncelle
            document.title = `${title} - ${artist} 🎵`;

            // Marquee aktivasyonu
            const container = document.querySelector('.marquee-container');
            if (container) {
                if (container.offsetWidth < marqueeContent.offsetWidth) {
                    container.classList.add('marquee-active');
                } else {
                    container.classList.remove('marquee-active');
                }
            }
        }
    }
});
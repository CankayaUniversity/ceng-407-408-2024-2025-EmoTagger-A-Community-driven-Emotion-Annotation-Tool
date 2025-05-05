

    // Play butonuna tıklandığında
    const playButton = document.getElementById('main-play-button');
    if (playButton) {
        playButton.addEventListener('click', function () {
            const activeSlide = document.querySelector('.swiper-slide-active');

            if (activeSlide) {
                const musicId = activeSlide.getAttribute('data-music-id');
                const title = activeSlide.getAttribute('data-title');
                const artist = activeSlide.getAttribute('data-artist');
                const filename = activeSlide.getAttribute('data-filename');

                if (musicId && filename) {
                    // Müziği çal
                    playMusic(musicId, title, artist, filename);
                }
            }
        });
    }

    // Müziği çal fonksiyonu
    function playMusic(musicId, title, artist, filename) {
        currentTrackId = musicId;

        // Müzik çalma işlemleri
        musicPlayer.src = `https://emomusicc.vercel.app/music/${encodeURIComponent(filename)}`;

        musicPlayer.play()
            .then(() => {
                // Play butonunu değiştir
                playButton.innerHTML = '<i class="bi bi-pause-fill"></i>';

                // Sayfa başlığını güncelle
                document.title = `${title} - ${artist}`;

                // Footer'daki şarkı bilgisini güncelle
                updateNowPlaying(title, artist);

                // Dinleme sayısını logla
                logPlayTrack(musicId);
            })
            .catch(error => {
                console.error('Müzik çalma hatası:', error);
                showNotification('Müzik çalınamadı', 'error');
            });
    }

    // Dinleme logunu gönder
    function logPlayTrack(musicId) {
        fetch('/Dashboard/LogPlayed', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify({ MusicId: musicId })
        })
            .then(response => {
                if (!response.ok) {
                    throw new Error('Sunucu hatası');
                }
                return response.json();
            })
            .then(data => {
                // Gerekirse ek işlemler yapılabilir
                updateRecentlyPlayed();
            })
            .catch(error => {
                console.error('Dinleme log hatası:', error);
            });
    }

    // Tag'leme fonksiyonu
    function tagTrack(musicId, tagName) {
        fetch('/Dashboard/SaveTag', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify({
                musicId: musicId,
                tagName: tagName
            })
        })
            .then(response => response.json())
            .then(data => {
                if (data.success) {
                    showNotification(`Şarkı "${tagName}" olarak etiketlendi.`, 'success');
                    updateRecentlyTagged();
                    updateTaggedMusicCount();
                }
            })
            .catch(error => {
                console.error('Etiketleme hatası:', error);
                showNotification('Etiketleme başarısız', 'error');
            });
    }

    // Tag seçimi
    const tags = document.querySelectorAll('.tag');
    tags.forEach(tag => {
        tag.addEventListener('click', function () {
            if (!currentTrackId) {
                showNotification('Önce bir müzik seçmelisiniz!', 'warning');
                return;
            }

            // Tüm tag'lerden selected sınıfını kaldır
            tags.forEach(t => t.classList.remove('selected'));

            // Bu tag'e selected sınıfını ekle
            this.classList.add('selected');

            // Tag bilgisini al
            const tagName = this.querySelector('span:not(.emoji)').textContent.trim();

            // Etiketleme işlemi
            tagTrack(currentTrackId, tagName);
        });
    });

    // Bildirim gösterme fonksiyonu
    function showNotification(message, type = 'info') {
        const notification = document.createElement('div');
        notification.className = `notification notification-${type}`;
        notification.textContent = message;

        document.body.appendChild(notification);

        setTimeout(() => {
            notification.classList.add('show');
            setTimeout(() => {
                notification.classList.remove('show');
                setTimeout(() => {
                    document.body.removeChild(notification);
                }, 300);
            }, 3000);
        }, 10);
    }

    // Yardımcı fonksiyonlar için placeholder
    function updateRecentlyPlayed() {
        // Gerekirse güncellenecek
    }

    function updateRecentlyTagged() {
        // Gerekirse güncellenecek
    }

    function updateTaggedMusicCount() {
        // Gerekirse güncellenecek
    }
});
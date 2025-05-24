// Global değişkenler - tek bir yerde tanımlanmalı
window.hasLogged = window.hasLogged || false;
window.lastLoggedId = window.lastLoggedId || null;
window.minimumPlayTime = window.minimumPlayTime || 3; // Kaydetmek için minimum dinleme süresi (saniye)
window.isGlobalLogging = window.isGlobalLogging || false; // Global loglama durumu (çift kayıtları önlemek için)

document.addEventListener('DOMContentLoaded', function () {
    console.log("Dinleme takip scripti başlatılıyor...");

    // Audio player'ı bul 
    const audioPlayer = document.getElementById('musicPlayer') ||
        window.parent.document.getElementById('musicPlayer');

    if (!audioPlayer) {
        console.error("Audio player bulunamadı!");
        return;
    }

    // Şarkı değiştiğinde geçmişi ve sayaçları sıfırla
    window.addEventListener('songChanged', function (event) {
        console.log("Şarkı değişti, loglama sıfırlanıyor:", event.detail);
        window.hasLogged = false;
        window.lastLoggedId = null;

        // Şarkı değiştiğinde geçmişi yenile
        setTimeout(() => {
            loadRecentlyPlayed();
            loadTagStats(event.detail.musicId || event.detail.id);
            updatePlayCounts(event.detail.musicId || event.detail.id);
        }, 500);
    });

    // Dinleme kaydı ve sayaç güncellemesi
    audioPlayer.addEventListener('timeupdate', function () {
        if (window.isGlobalLogging) return; // Eğer zaten bir loglama işlemi devam ediyorsa çık

        // Minimum dinleme süresini geçtiyse ve henüz loglanmadıysa
        if (!window.hasLogged &&
            this.currentTime > window.minimumPlayTime &&
            window.currentMusicId &&
            !this.paused) {

            // Loglama işlemi başlıyor
            window.isGlobalLogging = true;
            console.log(`Şarkı ${window.minimumPlayTime} saniyeden fazla çalındı, kaydediliyor: ID=${window.currentMusicId}`);

            // 1. Geçmişe kaydetme
            fetch('/Dashboard/LogPlayed', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ musicId: parseInt(window.currentMusicId) }),
                credentials: 'include'
            })
            .then(response => response.json())
            .then(data => {
                console.log("Dinleme geçmişi güncellendi:", data);
                // Geçmiş listesini yenile
                loadRecentlyPlayed();
            })
            .catch(err => console.error("Geçmiş kaydı hatası:", err));

            // 2. Dinleme sayacını güncelleme
            fetch('/PlayCounts/UpdateCounts', {
                method: 'POST',
                headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
                body: `musicId=${window.currentMusicId}`
            })
            .then(response => response.json())
            .then(data => {
                console.log("Dinleme sayacı güncellendi:", data);
                if (data.success) {
                    const totalCountEl = document.getElementById('totalPlayCount');
                    const userCountEl = document.getElementById('userPlayCount');
                    if (totalCountEl) totalCountEl.innerText = data.totalPlayCount;
                    if (userCountEl) userCountEl.innerText = data.userPlayCount;
                }
                // Loglama işlemi tamamlandı
                window.hasLogged = true;
                window.lastLoggedId = window.currentMusicId;
                window.isGlobalLogging = false;
            })
            .catch(err => {
                console.error("Sayaç güncelleme hatası:", err);
                window.isGlobalLogging = false;
            });
        }
    });

    // Sayfa yüklendiğinde ilk şarkı bilgilerini ve geçmişi yükle
    if (window.currentMusicId) {
        updatePlayCounts(window.currentMusicId);
        loadRecentlyPlayed();
    }
});

// Dinleme sayaçlarını getiren fonksiyon
function updatePlayCounts(musicId) {
    if (!musicId) return;

    fetch(`/PlayCounts/GetCounts?musicId=${musicId}`)
        .then(response => response.json())
        .then(data => {
            if (data.success) {
                // UI elemanlarını bul ve güncelle
                const totalCountEl = document.getElementById('totalPlayCount');
                const userCountEl = document.getElementById('userPlayCount');
                const playCountInfo = document.getElementById('playCountInfo');

                if (totalCountEl) totalCountEl.innerText = data.totalPlayCount;
                if (userCountEl) userCountEl.innerText = data.userPlayCount;
                if (playCountInfo) playCountInfo.style.display = 'flex';

                // Sayaçları localStorage'a kaydet
                window.savedTotalCount = data.totalPlayCount;
                window.savedUserCount = data.userPlayCount;
                window.savedCountMusicId = musicId;

                console.log("Dinleme sayaçları güncellendi:", data);
            }
        })
        .catch(err => console.error("Dinleme sayaçları alınamadı:", err));
}

// Son dinlenen parçaları yükle
function loadRecentlyPlayed() {
    console.log("Son dinlenenler yükleniyor...");

    fetch('/Dashboard/GetHistory')
        .then(res => {
            if (res.status === 401) {
                const tbody = document.getElementById('recentlyPlayedList');
                if (tbody) {
                    tbody.innerHTML = '<tr><td colspan="3">Geçmişi görmek için giriş yapmalısınız</td></tr>';
                }
                throw new Error("Oturum açılmamış");
            }

            return res.json();
        })
        .then(data => {
            console.log("Son dinlenenler yüklendi:", data);

            const tbody = document.getElementById('recentlyPlayedList');
            if (!tbody) {
                console.error("recentlyPlayedList bulunamadı!");
                return;
            }

            tbody.innerHTML = ''; // Tabloyu temizle

            if (data.played && data.played.length > 0) {
                // Verileri tabloya ekle
                data.played.forEach(item => {
                    const row = document.createElement('tr');
                    row.innerHTML = `
                        <td>${decodeHtmlEntities(item.title || 'Başlık Yok')}</td>
                        <td>${decodeHtmlEntities(item.artist || 'Sanatçı Yok')}</td>
                        <td>${item.playedAt || ''}</td>
                    `;
                    tbody.appendChild(row);
                });
            } else {
                // Veri yoksa bilgi mesajı göster
                const row = document.createElement('tr');
                row.innerHTML = '<td colspan="3">Henüz şarkı dinlenmedi.</td>';
                tbody.appendChild(row);
            }
        })
        .catch(err => {
            if (err.message !== "Oturum açılmamış") {
                console.error("Geçmiş yüklenirken hata:", err);
            }
        });
}

// HTML entity'leri decode etmek için yardımcı fonksiyon
function decodeHtmlEntities(text) {
    if (!text) return '';
    const textArea = document.createElement('textarea');
    textArea.innerHTML = text;
    return textArea.value;
}

// Yaygın bir kullanım için window objesine erişilebilir fonksiyonları bağla
window.updatePlayCounts = updatePlayCounts;
window.loadRecentlyPlayed = loadRecentlyPlayed;
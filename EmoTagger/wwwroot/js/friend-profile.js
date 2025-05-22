document.addEventListener('DOMContentLoaded', function () {
    console.log("✅ friend-profile.js yüklendi!");

    // Tab olay dinleyicilerini temizle ve yeniden ekle
    function setupTabs() {
        console.log("Sekme olay dinleyicileri ayarlanıyor...");

        // Önce tüm tab butonlarını bul
        const tabButtons = document.querySelectorAll('.profile-tabs .tab-link');
        const tabContents = document.querySelectorAll('.tab-content');

        // Olay dinleyicilerini temizle
        tabButtons.forEach(btn => {
            const newBtn = btn.cloneNode(true);
            btn.parentNode.replaceChild(newBtn, btn);
        });

        // Yeni seçicilerle yeniden seç
        const newTabButtons = document.querySelectorAll('.profile-tabs .tab-link');

        // Yeni olay dinleyicileri ekle
        newTabButtons.forEach(btn => {
            btn.addEventListener('click', function () {
                // Aktif sekme sınıflarını kaldır
                newTabButtons.forEach(b => b.classList.remove('active'));
                tabContents.forEach(tc => tc.classList.remove('active'));

                // Tıklanan sekmeyi ve içeriğini aktif yap
                btn.classList.add('active');
                const tabId = btn.getAttribute('data-tab');
                document.getElementById(tabId)?.classList.add('active');

                // İlgili içeriği yükle
                if (tabId === 'friends') {
                    loadFriends();
                } else if (tabId === 'requests') {
                    loadRequests();
                }
            });
        });

        // Eğer aktif bir sekme yoksa ilk sekmeyi aktif yap
        const activeTab = document.querySelector('.tab-link.active');
        if (!activeTab && newTabButtons.length > 0) {
            newTabButtons[0].classList.add('active');
            const firstTabId = newTabButtons[0].getAttribute('data-tab');
            document.getElementById(firstTabId)?.classList.add('active');
        }

        console.log("Sekme olay dinleyicileri eklendi!");
    }

    // İlk yükleme
    setupTabs();

    // Sayfa tamamen yüklendiğinde tekrar kontrol et
    window.addEventListener('load', setupTabs);

    // My Profile bağlantısına tıklandığında da kontrol et
    const profileLinks = document.querySelectorAll('a[href*="Profile"]');
    profileLinks.forEach(link => {
        link.addEventListener('click', () => {
            // Kısa bir gecikme ekleyerek sayfa değişikliğinden sonra çalışmasını sağla
            setTimeout(setupTabs, 500);
        });
    });

    // Arkadaşlar ve istekler ilk yükleme
    loadFriends();
    loadRequests();

    // Her 30 saniyede bir güncelle
    setInterval(() => {
        if (document.getElementById('friends').classList.contains('active')) {
            loadFriends();
        }
        if (document.getElementById('requests').classList.contains('active')) {
            loadRequests();
        }
    }, 30000);

    function loadFriends() {
        const list = document.getElementById('friends-list');
        if (!list) {
            console.warn("Arkadaş listesi elementi bulunamadı!");
            return;
        }

        // Yükleniyor göstergesi
        list.innerHTML = '<div class="text-center"><div class="spinner-border text-primary" role="status"></div><div class="mt-2">Arkadaşlar yükleniyor...</div></div>';

        fetch('/Dashboard/GetFriends')
            .then(res => res.json())
            .then(data => {
                if (!data.success || !data.friends.length) {
                    list.innerHTML = '<div class="text-muted">Hiç arkadaşın yok.</div>';
                    return;
                }
                list.innerHTML = data.friends.map(f => `
                    <div class="friend-item d-flex align-items-center justify-content-between mb-2">
                        <div class="d-flex align-items-center gap-2">
                            <img src="${f.profileImageUrl || '/img/default-avatar.png'}" class="rounded-circle" style="width:32px;height:32px;object-fit:cover;">
                            <span>${f.name}</span>
                            <span style="font-size:0.9em; margin-left:8px; color:${f.isOnline ? '#4CAF50' : '#e53935'};">
                                <span style="width:8px;height:8px;display:inline-block;border-radius:50%;background:${f.isOnline ? '#4CAF50' : '#e53935'};margin-right:4px;"></span>
                                ${f.isOnline ? 'Online' : 'Offline'}
                            </span>
                        </div>
                        <div>
                            <button class="btn btn-outline-danger btn-sm me-1" onclick="removeFriend(${f.id})">Sil</button>
                            <button class="btn btn-outline-dark btn-sm" onclick="blockFriend(${f.id})">Engelle</button>
                        </div>
                    </div>
                `).join('');
            })
            .catch(error => {
                console.error('Arkadaşlar yüklenirken hata:', error);
                list.innerHTML = '<div class="text-danger">Arkadaş listesi yüklenirken bir hata oluştu. Lütfen sayfayı yenileyin.</div>';
            });
    }

    function loadRequests() {
        const list = document.getElementById('requests-list');
        if (!list) {
            console.warn("İstekler listesi elementi bulunamadı!");
            return;
        }

        // Yükleniyor göstergesi
        list.innerHTML = '<div class="text-center"><div class="spinner-border text-primary" role="status"></div><div class="mt-2">İstekler yükleniyor...</div></div>';

        fetch('/Dashboard/GetIncomingFriendRequests')
            .then(res => res.json())
            .then(data => {
                const badge = document.getElementById('requests-count');
                const friendBadge = document.getElementById('friend-requests-count');

                if (!data.success || !data.requests.length) {
                    list.innerHTML = '<div class="text-muted">Gelen istek yok.</div>';
                    if (badge) badge.style.display = 'none';
                    if (friendBadge) friendBadge.style.display = 'none';
                    return;
                }

                // Badge'leri güncelle
                if (badge) {
                    badge.textContent = data.requests.length;
                    badge.style.display = '';
                }
                if (friendBadge) {
                    friendBadge.textContent = data.requests.length;
                    friendBadge.style.display = '';
                }

                // İstekleri listele
                list.innerHTML = data.requests.map(r => `
                    <div class="friend-item d-flex align-items-center justify-content-between mb-2">
                        <div class="d-flex align-items-center gap-2">
                            <img src="${r.profileImageUrl || '/img/default-avatar.png'}" class="rounded-circle" style="width:32px;height:32px;object-fit:cover;">
                            <span>${r.fromUserName}</span>
                        </div>
                        <div>
                            <button class="btn btn-success btn-sm me-1" onclick="acceptFriend(${r.requestId})">Kabul Et</button>
                            <button class="btn btn-outline-danger btn-sm" onclick="rejectFriend(${r.requestId})">Reddet</button>
                        </div>
                    </div>
                `).join('');
            })
            .catch(error => {
                console.error('İstekler yüklenirken hata:', error);
                list.innerHTML = '<div class="text-danger">İstekler yüklenirken bir hata oluştu. Lütfen sayfayı yenileyin.</div>';
            });
    }

    // Global arkadaş fonksiyonları
    window.acceptFriend = function (requestId) {
        fetch('/Dashboard/AcceptFriend', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ requestId })
        })
            .then(res => res.json())
            .then(data => {
                if (data.success) {
                    loadFriends();
                    loadRequests();
                } else {
                    alert("İstek kabul edilirken bir hata oluştu: " + (data.message || "Bilinmeyen hata"));
                }
            })
            .catch(error => {
                console.error('İstek kabul edilirken hata:', error);
                alert("Bir iletişim hatası oluştu. Lütfen tekrar deneyin.");
            });
    };

    window.rejectFriend = function (requestId) {
        fetch('/Dashboard/RejectFriend', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ requestId })
        })
            .then(res => res.json())
            .then(data => {
                if (data.success) {
                    loadRequests();
                } else {
                    alert("İstek reddedilirken bir hata oluştu: " + (data.message || "Bilinmeyen hata"));
                }
            })
            .catch(error => {
                console.error('İstek reddedilirken hata:', error);
                alert("Bir iletişim hatası oluştu. Lütfen tekrar deneyin.");
            });
    };

    window.removeFriend = function (friendId) {
        if (confirm("Bu arkadaşı silmek istediğinize emin misiniz?")) {
            fetch('/Dashboard/RemoveFriend', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ friendId })
            })
                .then(res => res.json())
                .then(data => {
                    if (data.success) {
                        loadFriends();
                    } else {
                        alert("Arkadaş silinirken bir hata oluştu: " + (data.message || "Bilinmeyen hata"));
                    }
                })
                .catch(error => {
                    console.error('Arkadaş silinirken hata:', error);
                    alert("Bir iletişim hatası oluştu. Lütfen tekrar deneyin.");
                });
        }
    };

    window.blockFriend = function (friendId) {
        if (confirm("Bu arkadaşı engellemek istediğinize emin misiniz?")) {
            fetch('/Dashboard/BlockFriend', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ friendId })
            })
                .then(res => res.json())
                .then(data => {
                    if (data.success) {
                        loadFriends();
                    } else {
                        alert("Arkadaş engellenirken bir hata oluştu: " + (data.message || "Bilinmeyen hata"));
                    }
                })
                .catch(error => {
                    console.error('Arkadaş engellenirken hata:', error);
                    alert("Bir iletişim hatası oluştu. Lütfen tekrar deneyin.");
                });
        }
    };
});
 
    console.log("✅ friend-profile.js yüklendi!");

    // Tab olay dinleyicilerini temizle ve yeniden ekle
    function setupTabs() {
        console.log("Sekme olay dinleyicileri ayarlanıyor...");

        // Önce tüm tab butonlarını bul
        const tabButtons = document.querySelectorAll('.profile-tabs .tab-link');
        const tabContents = document.querySelectorAll('.tab-content');

        // Olay dinleyicilerini temizle
        tabButtons.forEach(btn => {
            const newBtn = btn.cloneNode(true);
            btn.parentNode.replaceChild(newBtn, btn);
        });

        // Yeni seçicilerle yeniden seç
        const newTabButtons = document.querySelectorAll('.profile-tabs .tab-link');

        // Yeni olay dinleyicileri ekle
        newTabButtons.forEach(btn => {
            btn.addEventListener('click', function () {
                // Aktif sekme sınıflarını kaldır
                newTabButtons.forEach(b => b.classList.remove('active'));
                tabContents.forEach(tc => tc.classList.remove('active'));

                // Tıklanan sekmeyi ve içeriğini aktif yap
                btn.classList.add('active');
                const tabId = btn.getAttribute('data-tab');
                document.getElementById(tabId)?.classList.add('active');

                // İlgili içeriği yükle
                if (tabId === 'friends') {
                    loadFriends();
                } else if (tabId === 'requests') {
                    loadRequests();
                }
            });
        });

        // Eğer aktif bir sekme yoksa ilk sekmeyi aktif yap
        const activeTab = document.querySelector('.tab-link.active');
        if (!activeTab && newTabButtons.length > 0) {
            newTabButtons[0].classList.add('active');
            const firstTabId = newTabButtons[0].getAttribute('data-tab');
            document.getElementById(firstTabId)?.classList.add('active');
        }

        console.log("Sekme olay dinleyicileri eklendi!");
    }

    // İlk yükleme
    setupTabs();

    // Sayfa tamamen yüklendiğinde tekrar kontrol et
    window.addEventListener('load', setupTabs);

    // My Profile bağlantısına tıklandığında da kontrol et
    const profileLinks = document.querySelectorAll('a[href*="Profile"]');
    profileLinks.forEach(link => {
        link.addEventListener('click', () => {
            // Kısa bir gecikme ekleyerek sayfa değişikliğinden sonra çalışmasını sağla
            setTimeout(setupTabs, 500);
        });
    });

    // Arkadaşlar ve istekler ilk yükleme
    loadFriends();
    loadRequests();

    // Her 30 saniyede bir güncelle
    setInterval(() => {
        if (document.getElementById('friends').classList.contains('active')) {
            loadFriends();
        }
        if (document.getElementById('requests').classList.contains('active')) {
            loadRequests();
        }
    }, 30000);

    function loadFriends() {
        const list = document.getElementById('friends-list');
        if (!list) {
            console.warn("Arkadaş listesi elementi bulunamadı!");
            return;
        }

        // Yükleniyor göstergesi
        list.innerHTML = '<div class="text-center"><div class="spinner-border text-primary" role="status"></div><div class="mt-2">Arkadaşlar yükleniyor...</div></div>';

        fetch('/Dashboard/GetFriends')
            .then(res => res.json())
            .then(data => {
                if (!data.success || !data.friends.length) {
                    list.innerHTML = '<div class="text-muted">Hiç arkadaşın yok.</div>';
                    return;
                }
                list.innerHTML = data.friends.map(f => `
                    <div class="friend-item d-flex align-items-center justify-content-between mb-2">
                        <div class="d-flex align-items-center gap-2">
                            <img src="${f.profileImageUrl || '/img/default-avatar.png'}" class="rounded-circle" style="width:32px;height:32px;object-fit:cover;">
                            <span>${f.name}</span>
                            <span style="font-size:0.9em; margin-left:8px; color:${f.isOnline ? '#4CAF50' : '#e53935'};">
                                <span style="width:8px;height:8px;display:inline-block;border-radius:50%;background:${f.isOnline ? '#4CAF50' : '#e53935'};margin-right:4px;"></span>
                                ${f.isOnline ? 'Online' : 'Offline'}
                            </span>
                        </div>
                        <div>
                            <button class="btn btn-outline-danger btn-sm me-1" onclick="removeFriend(${f.id})">Sil</button>
                            <button class="btn btn-outline-dark btn-sm" onclick="blockFriend(${f.id})">Engelle</button>
                        </div>
                    </div>
                `).join('');
            })
            .catch(error => {
                console.error('Arkadaşlar yüklenirken hata:', error);
                list.innerHTML = '<div class="text-danger">Arkadaş listesi yüklenirken bir hata oluştu. Lütfen sayfayı yenileyin.</div>';
            });
    }

    function loadRequests() {
        const list = document.getElementById('requests-list');
        if (!list) {
            console.warn("İstekler listesi elementi bulunamadı!");
            return;
        }

        // Yükleniyor göstergesi
        list.innerHTML = '<div class="text-center"><div class="spinner-border text-primary" role="status"></div><div class="mt-2">İstekler yükleniyor...</div></div>';

        fetch('/Dashboard/GetIncomingFriendRequests')
            .then(res => res.json())
            .then(data => {
                const badge = document.getElementById('requests-count');
                const friendBadge = document.getElementById('friend-requests-count');

                if (!data.success || !data.requests.length) {
                    list.innerHTML = '<div class="text-muted">Gelen istek yok.</div>';
                    if (badge) badge.style.display = 'none';
                    if (friendBadge) friendBadge.style.display = 'none';
                    return;
                }

                // Badge'leri güncelle
                if (badge) {
                    badge.textContent = data.requests.length;
                    badge.style.display = '';
                }
                if (friendBadge) {
                    friendBadge.textContent = data.requests.length;
                    friendBadge.style.display = '';
                }

                // İstekleri listele
                list.innerHTML = data.requests.map(r => `
                    <div class="friend-item d-flex align-items-center justify-content-between mb-2">
                        <div class="d-flex align-items-center gap-2">
                            <img src="${r.profileImageUrl || '/img/default-avatar.png'}" class="rounded-circle" style="width:32px;height:32px;object-fit:cover;">
                            <span>${r.fromUserName}</span>
                        </div>
                        <div>
                            <button class="btn btn-success btn-sm me-1" onclick="acceptFriend(${r.requestId})">Kabul Et</button>
                            <button class="btn btn-outline-danger btn-sm" onclick="rejectFriend(${r.requestId})">Reddet</button>
                        </div>
                    </div>
                `).join('');
            })
            .catch(error => {
                console.error('İstekler yüklenirken hata:', error);
                list.innerHTML = '<div class="text-danger">İstekler yüklenirken bir hata oluştu. Lütfen sayfayı yenileyin.</div>';
            });
    }

    // Global arkadaş fonksiyonları
    window.acceptFriend = function (requestId) {
        fetch('/Dashboard/AcceptFriend', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ requestId })
        })
            .then(res => res.json())
            .then(data => {
                if (data.success) {
                    loadFriends();
                    loadRequests();
                } else {
                    alert("İstek kabul edilirken bir hata oluştu: " + (data.message || "Bilinmeyen hata"));
                }
            })
            .catch(error => {
                console.error('İstek kabul edilirken hata:', error);
                alert("Bir iletişim hatası oluştu. Lütfen tekrar deneyin.");
            });
    };

    window.rejectFriend = function (requestId) {
        fetch('/Dashboard/RejectFriend', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ requestId })
        })
            .then(res => res.json())
            .then(data => {
                if (data.success) {
                    loadRequests();
                } else {
                    alert("İstek reddedilirken bir hata oluştu: " + (data.message || "Bilinmeyen hata"));
                }
            })
            .catch(error => {
                console.error('İstek reddedilirken hata:', error);
                alert("Bir iletişim hatası oluştu. Lütfen tekrar deneyin.");
            });
    };

    window.removeFriend = function (friendId) {
        if (confirm("Bu arkadaşı silmek istediğinize emin misiniz?")) {
            fetch('/Dashboard/RemoveFriend', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ friendId })
            })
                .then(res => res.json())
                .then(data => {
                    if (data.success) {
                        loadFriends();
                    } else {
                        alert("Arkadaş silinirken bir hata oluştu: " + (data.message || "Bilinmeyen hata"));
                    }
                })
                .catch(error => {
                    console.error('Arkadaş silinirken hata:', error);
                    alert("Bir iletişim hatası oluştu. Lütfen tekrar deneyin.");
                });
        }
    };

    window.blockFriend = function (friendId) {
        if (confirm("Bu arkadaşı engellemek istediğinize emin misiniz?")) {
            fetch('/Dashboard/BlockFriend', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ friendId })
            })
                .then(res => res.json())
                .then(data => {
                    if (data.success) {
                        loadFriends();
                    } else {
                        alert("Arkadaş engellenirken bir hata oluştu: " + (data.message || "Bilinmeyen hata"));
                    }
                })
                .catch(error => {
                    console.error('Arkadaş engellenirken hata:', error);
                    alert("Bir iletişim hatası oluştu. Lütfen tekrar deneyin.");
                });
        }
    };
});
 
 
 
 
 
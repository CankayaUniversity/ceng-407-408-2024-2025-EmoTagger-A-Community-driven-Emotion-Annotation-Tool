document.addEventListener("DOMContentLoaded", function () {
    console.log("✅ profile-init.js yüklendi!");

    let isFormEdited = false;

    const editBtn = document.getElementById("editBtn");
    const saveBtn = document.getElementById("saveBtn");
    const cancelBtn = document.getElementById("cancelBtn");
    const inputs = document.querySelectorAll(".form-control");
    const profileForm = document.getElementById("profileForm");
    const uploadForm = document.getElementById("uploadForm");
    const profileImageInput = document.getElementById("profileImage");
    const uploadStatus = document.getElementById("uploadStatus");

    // === PROFİL DÜZENLEME ===
    if (editBtn && saveBtn && cancelBtn && inputs.length > 0) {
        editBtn.addEventListener("click", function () {
            inputs.forEach(input => input.removeAttribute("readonly"));
            editBtn.style.display = "none";
            saveBtn.style.display = "inline-block";
            cancelBtn.style.display = "inline-block";
        });

        cancelBtn.addEventListener("click", function () {
            profileForm.reset();
            inputs.forEach(input => input.setAttribute("readonly", "readonly"));
            editBtn.style.display = "inline-block";
            saveBtn.style.display = "none";
            cancelBtn.style.display = "none";
            isFormEdited = false;
        });

        inputs.forEach(input => {
            input.addEventListener("input", () => {
                isFormEdited = true;
            });
        });

        profileForm.addEventListener("submit", function (e) {
            if (!isFormEdited) {
                alert("Hiçbir değişiklik yapmadınız.");
                e.preventDefault();
                return false;
            }
        });

        window.addEventListener("beforeunload", function (e) {
            if (isFormEdited) {
                e.preventDefault();
                e.returnValue = '';
            }
        });
    }

    // === PROFİL RESMİ YÜKLEME ===
    if (profileImageInput) {
        profileImageInput.addEventListener("change", function () {
            const file = this.files[0];
            if (!file) return;

            if (!file.type.startsWith("image/")) {
                alert("Lütfen bir resim dosyası seçin.");
                this.value = '';
                return;
            }

            if (file.size > 5 * 1024 * 1024) {
                alert("Dosya boyutu çok büyük! 5MB sınırı var.");
                this.value = '';
                return;
            }

            const reader = new FileReader();
            reader.onload = function (e) {
                const profileImage = document.querySelector(".profile-image");
                if (profileImage) {
                    profileImage.src = e.target.result;
                }
            };
            reader.readAsDataURL(file);

            if (uploadStatus) {
                uploadStatus.style.display = "block";
                uploadStatus.innerHTML = '<span class="text-info">Resim yükleniyor...</span>';
            }

            uploadForm.submit();
        });
    }

    // === NAVBARDAN PROFİL FOTO GÜNCELLE ===
    if (typeof window.updateProfileImageInNavbar === "function") {
        const currentProfileImage = document.querySelector(".profile-image")?.src;
        if (currentProfileImage) {
            window.updateProfileImageInNavbar(currentProfileImage);
        }
    }

    // === RESİM CACHE BUSTER ===
    const urlParams = new URLSearchParams(window.location.search);
    if (urlParams.has("refresh")) {
        const allImages = document.querySelectorAll("img");
        allImages.forEach(img => {
            if (img.src && !img.src.includes("?v=")) {
                img.src += "?v=" + new Date().getTime();
            }
        });

        if (window.parent && window.parent !== window) {
            window.parent.location.reload();
        }
    }

    // === MÜZİK İSTATİSTİKLERİ ===
    fetch("/Dashboard/GetUserMusicStats")
        .then(res => res.json())
        .then(data => {
            if (!data.success) return;

            document.getElementById("mostTaggedGenre").innerHTML =
                `<span>${data.mostTaggedGenre} (${data.mostTaggedCount})</span>`;
            document.getElementById("totalTagCount").innerHTML =
                `<span>${data.totalTagCount}</span>`;
            document.getElementById("totalPlayedMusic").innerHTML =
                `<span>${data.totalPlayedMusic}</span>`;
            document.getElementById("favoriteCount").innerHTML =
                `<span>${data.favoriteCount}</span>`;
        })
        .catch(error => {
            console.error("Müzik istatistikleri alınamadı:", error);
        });
});
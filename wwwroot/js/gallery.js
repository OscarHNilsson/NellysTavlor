let currentIndex = 0;

function openModal(src) {
    currentIndex = galleryImages.indexOf(src);
    updateModalImage();
    document.getElementById('imageModal').classList.add('active');
}

function closeModal() {
    document.getElementById('imageModal').classList.remove('active');
}

function showNext() {
    currentIndex = (currentIndex + 1) % galleryImages.length;
    updateModalImage();
}

function showPrev() {
    currentIndex = (currentIndex - 1 + galleryImages.length) % galleryImages.length;
    updateModalImage();
}

function updateModalImage() {
    document.getElementById('modalImage').src = galleryImages[currentIndex];
}

document.addEventListener('keydown', function (e) {
    const modal = document.getElementById('imageModal');
    if (!modal.classList.contains('active')) return;

    if (e.key === 'Escape') closeModal();
    if (e.key === 'ArrowRight') showNext();
    if (e.key === 'ArrowLeft') showPrev();
});
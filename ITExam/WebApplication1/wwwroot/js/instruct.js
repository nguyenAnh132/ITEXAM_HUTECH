

(() => {
    const pageRoot = document.getElementById('huongdanPage');
    if (!pageRoot) return;

    const searchInput = document.getElementById('hd-search');
    const studentOuter = document.getElementById('cl-student');
    const teacherOuter = document.getElementById('cl-teacher');
    const noticeOuter = document.getElementById('cl-notice');

    // Tất cả item con trong 2 nhóm
    const items = pageRoot.querySelectorAll('#accStudent .accordion-item, #accTeacher .accordion-item');

    const getCollapse = (el) => bootstrap.Collapse.getOrCreateInstance(el, { toggle: false });
    const openOuter = (el) => el && getCollapse(el).show();
    const closeOuter = (el) => el && getCollapse(el).hide();

    const closeAllInner = () => {
        pageRoot.querySelectorAll('#accStudent .accordion-collapse, #accTeacher .accordion-collapse')
            .forEach(el => getCollapse(el).hide());
    };

    const strip = (s) =>
        (s || '')
            .normalize('NFD')
            .replace(/[\u0300-\u036f]/g, '')
            .replace(/đ/g, 'd').replace(/Đ/g, 'D')
            .toLowerCase()
            .trim();

    const clearMarks = () => {
        pageRoot.querySelectorAll('.hd-hit').forEach(el => el.classList.remove('hd-hit'));
        pageRoot.querySelectorAll('.hd-hit-body').forEach(el => el.classList.remove('hd-hit-body'));
    };

    const doFilter = (raw) => {
        const q = (raw || '').trim();
        clearMarks();

        // Nếu chuỗi < 3 ký tự => không lọc (tránh trùng lan man như "ma")
        if (q.length < 3) {
            items.forEach(i => i.classList.remove('d-none'));
            closeAllInner();
            closeOuter(studentOuter);
            closeOuter(teacherOuter);
            closeOuter(noticeOuter);
            return;
        }

        const qPlain = strip(q);

        // Ẩn tất cả trước
        items.forEach(i => i.classList.add('d-none'));

        let foundStudent = false;
        let foundTeacher = false;

        items.forEach(item => {
            const headerBtn = item.querySelector('.accordion-button');
            const bodyEl = item.querySelector('.accordion-body');
            const titlePlain = strip(headerBtn ? headerBtn.innerText : '');
            const bodyPlain = strip(bodyEl ? bodyEl.innerText : '');

            const hitTitle = titlePlain.includes(qPlain);     // khớp cụm từ đầy đủ
            const hitBody = !hitTitle && bodyPlain.includes(qPlain);

            if (hitTitle || hitBody) {
                item.classList.remove('d-none');

                // Mở panel con nếu trúng (dù trúng tiêu đề hay nội dung)
                const innerCollapse = item.querySelector('.accordion-collapse');
                if (innerCollapse) getCollapse(innerCollapse).show();

                // Tô sáng tiêu đề theo kiểu trúng
                if (headerBtn) headerBtn.classList.add(hitTitle ? 'hd-hit' : 'hd-hit-body');

                if (item.closest('#accStudent')) foundStudent = true;
                if (item.closest('#accTeacher')) foundTeacher = true;
            }
        });

        // Mở nhóm ngoài có kết quả, đóng nhóm không có
        if (foundStudent) openOuter(studentOuter); else closeOuter(studentOuter);
        if (foundTeacher) openOuter(teacherOuter); else closeOuter(teacherOuter);

        // Nhóm "Đáng chú ý" không tham gia lọc => đóng khi tìm kiếm
        closeOuter(noticeOuter);
    };

    if (searchInput) {
        searchInput.addEventListener('input', (e) => doFilter(e.target.value));
        searchInput.addEventListener('keydown', (e) => {
            if (e.key === 'Escape') {
                searchInput.value = '';
                doFilter('');
            }
        });
    }

    // Khởi tạo mặc định
    doFilter('');
})();

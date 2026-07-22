(() => {
    'use strict';

    const form = document.getElementById('productSearchForm');
    const input = document.getElementById('productSearchInput');
    const results = document.getElementById('productSearchResults');
    const clearButton = document.getElementById('productSearchClear');
    const feedback = document.getElementById('productSearchFeedback');

    if (!form || !input || !results || !form.dataset.searchUrl) return;

    let timer;
    let request;

    const variationRows = group =>
        results.querySelectorAll(`[data-variation-row="${CSS.escape(group)}"]`);

    const setGroupExpanded = (button, expanded) => {
        const group = button.dataset.variationGroup;
        if (!group) return;

        variationRows(group).forEach(row => { row.hidden = !expanded; });
        button.setAttribute('aria-expanded', String(expanded));

        const icon = button.querySelector('i');
        icon?.classList.toggle('bi-chevron-right', !expanded);
        icon?.classList.toggle('bi-chevron-down', expanded);
    };

    const expandAll = () => {
        results.querySelectorAll('.product-variations-toggle')
            .forEach(button => setGroupExpanded(button, true));
    };

    results.addEventListener('click', event => {
        const button = event.target.closest('.product-variations-toggle');
        if (!button) return;

        setGroupExpanded(button, button.getAttribute('aria-expanded') !== 'true');
    });

    const updateUrl = term => {
        const url = new URL(window.location.href);
        url.searchParams.delete('handler');
        if (term) url.searchParams.set('Pesquisa', term);
        else url.searchParams.delete('Pesquisa');
        history.replaceState(null, '', url);
    };

    const search = async () => {
        const term = input.value.trim();
        request?.abort();
        request = new AbortController();

        const url = new URL(form.dataset.searchUrl, window.location.origin);
        url.searchParams.set('pesquisa', term);

        results.setAttribute('aria-busy', 'true');
        form.classList.add('product-search-loading');
        feedback.textContent = 'Pesquisando produtos...';

        try {
            const response = await fetch(url, {
                headers: { 'X-Requested-With': 'XMLHttpRequest' },
                signal: request.signal
            });
            if (!response.ok) throw new Error(`Falha HTTP ${response.status}`);

            results.innerHTML = await response.text();
            if (term) expandAll();

            clearButton?.classList.toggle('d-none', !term);
            updateUrl(term);
            feedback.textContent = 'Listagem de produtos atualizada.';
        } catch (error) {
            if (error.name !== 'AbortError') {
                feedback.textContent = 'Não foi possível atualizar a pesquisa. Use o botão Pesquisar.';
                console.error('Falha ao pesquisar produtos:', error);
            }
        } finally {
            results.removeAttribute('aria-busy');
            form.classList.remove('product-search-loading');
        }
    };

    input.addEventListener('input', () => {
        clearTimeout(timer);
        timer = window.setTimeout(search, 250);
    });

    form.addEventListener('submit', event => {
        event.preventDefault();
        clearTimeout(timer);
        search();
    });

    clearButton?.addEventListener('click', event => {
        event.preventDefault();
        input.value = '';
        input.focus();
        clearTimeout(timer);
        search();
    });

    if (results.dataset.autoExpand === 'true') expandAll();
})();

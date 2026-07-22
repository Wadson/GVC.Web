(() => {
    const toggle = document.getElementById('temVariacaoSwitch');
    if (!toggle) return;
    const area = document.getElementById('variationArea');
    const body = document.getElementById('variationsBody');
    const definitions = document.getElementById('attributeDefinitions');
    const parentFields = ['Produto_GtinEan', 'Produto_Estoque'].map(id => document.getElementById(id)).filter(Boolean);

    const reindex = () => body.querySelectorAll('[data-variation-row]').forEach((row, index, rows) => {
        const setName = (selector, property) => { const field = row.querySelector(selector); if (field) field.name = `Variacoes[${index}].${property}`; };
        setName('[data-variation-id],input[name$=".VariacaoId"]', 'VariacaoId');
        setName('[data-sku],input[name$=".Sku"]', 'Sku'); setName('[data-gtin],input[name$=".GtinEan"]', 'GtinEan');
        setName('[data-cost],input[name$=".PrecoCusto"]', 'PrecoCusto'); setName('[data-price],input[name$=".PrecoDeVenda"]', 'PrecoDeVenda');
        setName('[data-stock],input[name$=".Estoque"]', 'Estoque'); setName('[data-status],select[name$=".Status"]', 'Status');
        setName('[data-current-image],input[name$=".ImagemAtual"]', 'ImagemAtual');
        setName('[data-remove-image],input[name$=".RemoverImagem"]', 'RemoverImagem');
        setName('[data-variation-image],input[name$=".ArquivoImagem"]', 'ArquivoImagem');
        row.querySelectorAll('[data-attribute-name]').forEach((field, attributeIndex) => field.name = `Variacoes[${index}].Atributos[${attributeIndex}].NomeAtributo`);
        row.querySelectorAll('[data-attribute-value]').forEach((field, attributeIndex) => field.name = `Variacoes[${index}].Atributos[${attributeIndex}].ValorAtributo`);
        row.querySelector('.copy-first-prices')?.classList.toggle('d-none', index !== 0 || rows.length < 2);
    });
    const update = () => { area.classList.toggle('d-none', !toggle.checked); parentFields.forEach(field => field.disabled = toggle.checked); };
    const addDefinition = () => { const item = document.getElementById('attributeDefinitionTemplate').content.firstElementChild.cloneNode(true); definitions.appendChild(item); item.querySelector('.remove-attribute').onclick = () => item.remove(); };
    const combinations = groups => groups.reduce((result, group) => result.flatMap(current => group.values.map(value => [...current, { name: group.name, value }])), [[]]);
    const slug = value => value.normalize('NFD').replace(/[\u0300-\u036f]/g, '').replace(/[^a-z0-9]+/gi, '-').replace(/^-|-$/g, '').toUpperCase();
    const addVariation = attributes => {
        const row = document.getElementById('variationRowTemplate').content.firstElementChild.cloneNode(true);
        row.querySelector('[data-attribute-summary]').textContent = attributes.map(x => `${x.name}: ${x.value}`).join(' / ');
        const host = row.querySelector('[data-attributes-hidden]');
        attributes.forEach(attribute => { host.insertAdjacentHTML('beforeend', `<input type="hidden" data-attribute-name value="${attribute.name.replace(/"/g, '&quot;')}"><input type="hidden" data-attribute-value value="${attribute.value.replace(/"/g, '&quot;')}">`); });
        const reference = document.getElementById('Produto_Referencia')?.value?.trim() || 'SKU';
        row.querySelector('[data-sku]').value = `${slug(reference)}-${attributes.map(x => slug(x.value)).join('-')}`;
        body.appendChild(row); reindex();
    };
    document.getElementById('addAttribute').onclick = addDefinition;
    document.getElementById('generateVariations').onclick = () => {
        const groups = [...definitions.querySelectorAll('.attribute-definition')].map(row => ({ name: row.querySelector('.attribute-name').value.trim(), values: row.querySelector('.attribute-values').value.split(',').map(x => x.trim()).filter(Boolean) })).filter(x => x.name && x.values.length);
        if (!groups.length) { window.alert('Informe ao menos um atributo e seus valores.'); return; }
        if (body.children.length && !window.confirm('Substituir a grade atual pelas novas combinações?')) return;
        body.innerHTML = ''; combinations(groups).forEach(addVariation);
    };
    const fieldsForRow = row => ({
        cost: row.querySelector('[data-cost],input[name$=".PrecoCusto"]'),
        price: row.querySelector('[data-price],input[name$=".PrecoDeVenda"]')
    });
    document.getElementById('applyVariationPrices').onclick = () => {
        const cost = document.getElementById('bulkVariationCost').value;
        const price = document.getElementById('bulkVariationPrice').value;
        if (cost === '' && price === '') { window.alert('Informe o custo e/ou o preço de venda.'); return; }
        body.querySelectorAll('[data-variation-row]').forEach(row => {
            const fields = fieldsForRow(row);
            if (cost !== '') fields.cost.value = cost;
            if (price !== '') fields.price.value = price;
        });
    };
    const copyFirstPrices = () => {
        const rows = [...body.querySelectorAll('[data-variation-row]')];
        if (rows.length < 2) { window.alert('Gere ao menos duas variações.'); return; }
        const first = fieldsForRow(rows[0]);
        rows.slice(1).forEach(row => {
            const fields = fieldsForRow(row);
            fields.cost.value = first.cost.value;
            fields.price.value = first.price.value;
        });
    };
    document.getElementById('copyFirstVariationPrices').onclick = copyFirstPrices;
    const resetVariationImage = row => {
        const input = row.querySelector('[data-variation-image]');
        const preview = row.querySelector('[data-variation-preview]');
        if (preview.dataset.objectUrl) URL.revokeObjectURL(preview.dataset.objectUrl);
        input.value = '';
        preview.src = '/images/no-product.svg';
        preview.removeAttribute('data-object-url');
        row.querySelector('[data-remove-image]').value = 'true';
    };
    body.addEventListener('click', event => {
        const row = event.target.closest('[data-variation-row]');
        if (!row) return;
        if (event.target.closest('.copy-first-prices')) copyFirstPrices();
        if (event.target.closest('.choose-variation-image')) row.querySelector('[data-variation-image]').click();
        if (event.target.closest('.remove-variation-image')) resetVariationImage(row);
    });
    body.addEventListener('change', event => {
        if (!event.target.matches('[data-variation-image]')) return;
        const row = event.target.closest('[data-variation-row]');
        const file = event.target.files?.[0];
        if (!file) return;
        const preview = row.querySelector('[data-variation-preview]');
        if (preview.dataset.objectUrl) URL.revokeObjectURL(preview.dataset.objectUrl);
        const objectUrl = URL.createObjectURL(file);
        preview.src = objectUrl;
        preview.dataset.objectUrl = objectUrl;
        row.querySelector('[data-remove-image]').value = 'false';
    });
    body.addEventListener('click', event => { const button = event.target.closest('.remove-variation'); if (button) { button.closest('tr').remove(); reindex(); } });
    toggle.addEventListener('change', update); addDefinition(); update(); reindex();
})();

(() => {
    'use strict';

    const onlyDigits = value => (value || '').replace(/\D/g, '');
    const normalize = value => (value || '')
        .normalize('NFD')
        .replace(/[\u0300-\u036f]/g, '')
        .trim()
        .toLocaleLowerCase('pt-BR');

    const setFieldValue = (field, value) => {
        if (!field) return;
        field.value = value || '';
        field.dispatchEvent(new Event('input', { bubbles: true }));
        field.dispatchEvent(new Event('change', { bubbles: true }));
    };

    const findInForm = (cepInput, selector) => cepInput.form?.querySelector(selector)
        || document.querySelector(selector);

    const createFeedback = cepInput => {
        let feedback = cepInput.parentElement?.querySelector('[data-cep-feedback]');
        if (feedback) return feedback;

        feedback = document.createElement('div');
        feedback.dataset.cepFeedback = '';
        feedback.className = 'form-text';
        feedback.setAttribute('role', 'status');
        feedback.setAttribute('aria-live', 'polite');
        cepInput.insertAdjacentElement('afterend', feedback);
        return feedback;
    };

    const setFeedback = (feedback, message = '', type = 'secondary') => {
        feedback.textContent = message;
        feedback.className = `form-text text-${type}`;
    };

    const matchCity = async (cityName, uf, signal) => {
        const response = await fetch(`/Cidades/Buscar?termo=${encodeURIComponent(cityName)}`, {
            headers: { Accept: 'application/json' },
            signal
        });

        if (!response.ok) throw new Error('Não foi possível consultar as cidades cadastradas.');

        const cities = await response.json();
        return cities.find(city => normalize(city.nome) === normalize(cityName)
            && normalize(city.uf) === normalize(uf));
    };

    const initialize = cepInput => {
        if (cepInput.dataset.cepReady) return;
        cepInput.dataset.cepReady = 'true';

        const form = cepInput.form;
        const street = findInForm(cepInput, '[data-cep-logradouro], #Logradouro, #Endereco');
        const number = findInForm(cepInput, '[data-cep-numero], #Numero');
        const district = findInForm(cepInput, '[data-cep-bairro], #Bairro');
        const cityId = findInForm(cepInput, '[data-cep-cidade-id], #CidadeID');
        const state = findInForm(cepInput, '[data-cep-uf], #UF');
        const citySearch = cityId
            ? form?.querySelector(`[data-city-target="${CSS.escape(cityId.id)}"]`)
            : null;
        const feedback = createFeedback(cepInput);
        const addressFields = [street, district, citySearch, state].filter(Boolean);
        let controller;
        let lastCep = '';

        const setLoading = loading => {
            cepInput.setAttribute('aria-busy', String(loading));
            addressFields.forEach(field => {
                field.readOnly = loading;
                field.setAttribute('aria-busy', String(loading));
            });
            if (loading) setFeedback(feedback, 'Buscando CEP…', 'info');
        };

        const search = async () => {
            const cep = onlyDigits(cepInput.value);

            if (!cep) {
                lastCep = '';
                setFeedback(feedback);
                return;
            }

            if (cep.length !== 8) {
                lastCep = '';
                setFeedback(feedback, 'Informe um CEP com 8 dígitos.', 'danger');
                return;
            }

            if (cep === lastCep) return;
            lastCep = cep;
            controller?.abort();
            controller = new AbortController();
            setLoading(true);

            try {
                const response = await fetch(`https://viacep.com.br/ws/${cep}/json/`, {
                    headers: { Accept: 'application/json' },
                    signal: controller.signal
                });

                if (!response.ok) throw new Error('Não foi possível consultar o ViaCEP.');

                const address = await response.json();
                if (address.erro === true) {
                    lastCep = '';
                    setFeedback(feedback, 'CEP não encontrado. Preencha o endereço manualmente.', 'danger');
                    return;
                }

                setFieldValue(street, address.logradouro);
                setFieldValue(district, address.bairro);
                setFieldValue(state, address.uf);

                const city = await matchCity(address.localidade, address.uf, controller.signal);
                if (city) {
                    // A busca limpa o ID ao receber o evento do campo de texto.
                    // Grave o ID por último para preservar a cidade encontrada pelo CEP.
                    setFieldValue(citySearch, `${city.nome} - ${city.uf}`);
                    setFieldValue(cityId, city.id);
                    setFeedback(feedback, 'Endereço preenchido automaticamente.', 'success');
                } else {
                    setFieldValue(cityId, '');
                    setFieldValue(citySearch, `${address.localidade} - ${address.uf}`);
                    setFeedback(feedback, 'Endereço localizado, mas selecione a cidade na lista do sistema.', 'warning');
                }

                number?.focus();
            } catch (error) {
                if (error.name === 'AbortError') return;
                lastCep = '';
                setFeedback(feedback, 'Não foi possível buscar o CEP. Preencha o endereço manualmente.', 'danger');
            } finally {
                setLoading(false);
            }
        };

        cepInput.addEventListener('blur', search);
        cepInput.addEventListener('input', () => {
            const cep = onlyDigits(cepInput.value);
            if (cep.length === 8) search();
            else if (cep.length < 8) {
                lastCep = '';
                setFeedback(feedback);
            }
        });
    };

    const initializeAll = () => document
        .querySelectorAll('[data-cep-autocomplete], #Cep, #CEP')
        .forEach(initialize);

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', initializeAll);
    } else {
        initializeAll();
    }

    window.gvcCepAutocomplete = initializeAll;
})();

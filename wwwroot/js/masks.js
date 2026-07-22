(function () {
    'use strict';

    /* Track cursor position through digits only so deleting mask
       characters (dots, dashes, slashes, parentheses) doesn't
       misalign the caret. */
    function applyMask(input, maskFn) {
        var start = input.selectionStart;
        var raw = input.value.replace(/\D/g, '');
        var digitPos = 0;
        var i;
        for (i = 0; i < start; i++) {
            if (/\d/.test(input.value[i])) digitPos++;
        }
        var prevLen = input.value.length;
        input.value = maskFn(raw);
        var newLen = input.value.length;
        /* Walk forward through the new value to the same digit position */
        var newPos = 0;
        for (i = 0; i < digitPos && newPos < newLen; i++) {
            newPos++;
            while (newPos < newLen && !/\d/.test(input.value[newPos])) newPos++;
        }
        try { input.setSelectionRange(newPos, newPos); } catch (_) { }
    }

    function cpfMask(v) {
        v = v.replace(/\D/g, '').slice(0, 11);
        if (v.length <= 3) return v;
        if (v.length <= 6) return v.slice(0, 3) + '.' + v.slice(3);
        if (v.length <= 9) return v.slice(0, 3) + '.' + v.slice(3, 6) + '.' + v.slice(6);
        return v.slice(0, 3) + '.' + v.slice(3, 6) + '.' + v.slice(6, 9) + '-' + v.slice(9);
    }

    function cnpjMask(v) {
        v = v.replace(/\D/g, '').slice(0, 14);
        if (v.length <= 2) return v;
        if (v.length <= 5) return v.slice(0, 2) + '.' + v.slice(2);
        if (v.length <= 8) return v.slice(0, 2) + '.' + v.slice(2, 5) + '.' + v.slice(5);
        if (v.length <= 12) return v.slice(0, 2) + '.' + v.slice(2, 5) + '.' + v.slice(5, 8) + '/' + v.slice(8);
        return v.slice(0, 2) + '.' + v.slice(2, 5) + '.' + v.slice(5, 8) + '/' + v.slice(8, 12) + '-' + v.slice(12);
    }

    var masks = { cpf: cpfMask, cnpj: cnpjMask };
    var maskMaxLengths = { cpf: 14, cnpj: 18 };

    function unmaskForm(form) {
        var maskedInputs = Array.prototype.slice.call(form.querySelectorAll('[data-mask]'));

        maskedInputs.forEach(function (input) {
            input.value = input.value.replace(/\D/g, '');
        });

        // Se uma validação client-side impedir o envio, restaure a apresentação.
        window.setTimeout(function () {
            maskedInputs.forEach(function (input) {
                var fn = masks[input.dataset.mask];
                if (fn && input.isConnected) applyMask(input, fn);
            });
        }, 0);
    }

    function init() {
        document.querySelectorAll('[data-mask]').forEach(function (input) {
            if (input.dataset.maskReady) return;
            input.dataset.maskReady = '1';

            var fn = masks[input.dataset.mask];
            if (!fn) return;

            // StringLength gera maxlength com o tamanho do valor sem máscara.
            // No campo, o limite precisa também comportar a pontuação visual.
            input.maxLength = maskMaxLengths[input.dataset.mask];
            input.addEventListener('input', function () { applyMask(input, fn); });
            if (input.value) applyMask(input, fn);
        });

        if (!document.documentElement.dataset.maskSubmitReady) {
            document.documentElement.dataset.maskSubmitReady = '1';
            document.addEventListener('submit', function (event) {
                unmaskForm(event.target);
            }, true);
        }
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }
})();

window.crmThemeSettings = {
    load: function (key) {
        try {
            return window.localStorage.getItem(key);
        }
        catch {
            return null;
        }
    },
    save: function (key, value) {
        try {
            window.localStorage.setItem(key, value);
        }
        catch {
        }
    },
    downloadFile: function (fileName, contentType, contentBase64) {
        try {
            const link = document.createElement("a");
            link.href = `data:${contentType};base64,${contentBase64}`;
            link.download = fileName;
            document.body.appendChild(link);
            link.click();
            document.body.removeChild(link);
        }
        catch {
        }
    }
};

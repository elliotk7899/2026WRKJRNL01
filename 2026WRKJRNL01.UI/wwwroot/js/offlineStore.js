
window.offlineStore = {
    dbName: "OfflineDB",
    storeName: "workItems",

    async init() {
        const openReq = indexedDB.open(this.dbName, 1);
        openReq.onupgradeneeded = (e) => {
            const db = e.target.result;
            if (!db.objectStoreNames.contains(this.storeName)) {
                db.createObjectStore(this.storeName);
            }
        };
    },

    async getDb() {
        return new Promise((resolve, reject) => {
            const openReq = indexedDB.open(this.dbName, 1);
            openReq.onsuccess = () => resolve(openReq.result);
            openReq.onerror = () => reject(openReq.error);
        });
    },

    async saveItem(key, valueJson) {
        const db = await this.getDb();
        const tx = db.transaction(this.storeName, "readwrite");
        tx.objectStore(this.storeName).put(valueJson, key);
        return new Promise((resolve, reject) => {
            tx.oncomplete = () => resolve(true);
            tx.onerror = () => reject(tx.error);
        });
    },

    async getAllItems() {
        const db = await this.getDb();
        const tx = db.transaction(this.storeName, "readonly");
        const store = tx.objectStore(this.storeName);

        return new Promise((resolve, reject) => {
            const items = [];
            const req = store.openCursor();
            req.onsuccess = (e) => {
                const cursor = e.target.result;
                if (cursor) {
                    items.push(JSON.parse(cursor.value));
                    cursor.continue();
                } else {
                    resolve(JSON.stringify(items));
                }
            };
            req.onerror = () => reject(req.error);
        });
    }
};

window.offlineStore.init();

function resolver(incomingItem, existingItem, isTombstone, conflictingItems) {

        if (incomingItem.regionSrc === 'West US') {
            return; // existing item wins
        }

        // incoming item wins - clear conflicts and replace existing with incoming.
        tryDelete(conflictingItems, incomingItem, existingItem);
    

    function tryDelete(documents, incoming, existing) {
        if (documents.length > 0) {
            collection.deleteDocument(documents[0]._self, {}, function (err, responseOptions) {
                if (err) throw err;

                documents.shift();
                tryDelete(documents, incoming, existing);
            });
        } else if (existing) {
            collection.replaceDocument(existing._self, incoming,
                function (err, documentCreated) {
                    if (err) throw err;
                });
        } else {
            collection.createDocument(collection.getSelfLink(), incoming,
                function (err, documentCreated) {
                    if (err) throw err;
                });
        }
    }
}
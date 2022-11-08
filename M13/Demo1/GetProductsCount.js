function GetProductsCount() {
    var collection = getContext().getCollection();
    
    collection.queryDocuments(
        collection.getSelfLink(),
        'SELECT VALUE o.OrderItems FROM o',
        
        function (err, items, options) {
            var respond = '';
            if (err) throw err;
            
            if (!items || !items.length || !items[0].length)
                respond = 'no products found';
            else
            {
               var artTypes = {};               
               items.forEach(PrintProducts);
               respond += JSON.stringify(artTypes);
               
               function PrintProducts(prod) {
                    for(i=0; i<prod.length; i++)
                    {
                        var key = prod[i]["ProductItem"]["ProductName"];
                        if (artTypes[key] != null )  artTypes[key]+=parseInt(prod[i]["Count"]); else artTypes[key] =parseInt(prod[i]["Count"]);
                    }                   
                }    
            }
            getContext().getResponse().setBody(respond);
        });
}

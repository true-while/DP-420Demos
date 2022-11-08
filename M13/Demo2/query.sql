SELECT o.OrderNumber,o.OrderCustomer.Name
FROM  o
WHERE udf.Match(o.OrderCustomer.Name,"[Ll]evel") and udf.Match(o.OrderNumber,"NL-[0-5]{3}")
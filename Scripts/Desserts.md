# Сладости

Миграция 20261109160000_AddDesserts добавляет Desserts и DessertPurchases.
В рабочую БД вносится только новая миграция, предварительно создаётся дамп:

```powershell
dotnet run --project Scripts/Validation/SchemaChecks.csproj -- apply
```

Справочник Desserts:

| Поле | Значение |
| --- | --- |
| id | ID сладости |
| name | Название |
| asset_name | Ключ Unity-каталога |
| batch_size | 3 штуки на подносе |
| batch_price | Стоимость полного подноса |
| sale_price | Цена продажи одной сладости |

Булочка, печенье, медовик и моти добавляются самой миграцией: batch_price = 10,
sale_price = 6. Для изменения цен используйте UPDATE справочника, например:

```sql
UPDATE "Desserts" SET batch_price = 10, sale_price = 6
WHERE asset_name = 'dessert.cookie';
```

GET /desserts возвращает каталог. POST /desserts/purchase принимает save_id,
dessert_id, purchase_id (UUID). Покупка проверяет баланс и записывает квитанцию
в одной транзакции. Кошелёк блокируется на время операции; повтор той же квитанции
возвращает прежнюю партию без повторного списания. Ответ: status, purchase_id,
save_id, dessert_id, asset_name, quantity, spent_coins, sale_price, coins.
Клиент не передаёт стоимость. При нехватке монет ответ status=false.

DessertPurchases содержит квитанции, а не постоянный инвентарь. Остатки сладостей
и слоты игрока в текущей версии остаются локальными для сцены Unity. Выручка
сохраняется существующим POST /levels/complete по завершении смены.

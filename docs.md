# Documentacion

## Estructura api

* /Config
* /Controllers
* /Data

  * /Migrations
* /Services

  * /Impl

## Dependecias

* Bcrypt
* Jwt
* Ef core
* Ef tools
* Ef design
* Ef PostgreSQL
* Swagger

## Auth

Se usa el metodo de access tokens y refresh tokens con la libreria de JWT. Se retorna usando httpOnly cookies. El request a un endpoint con autorizacion debe tener el campo de 'X-Access-Token'.

## Miscellaneous for now

* One ticket per NON-MANUAL only payment.
* In each delete service method, also delete all their depending entities
* Usar .IgnoreQueryFilters() en las querys donde tenemos que buscar entidades que pueden tener padres eliminados. Esto seria el caso de los pagos y tickets, en los cuales nunca se aplica soft delete. Al usar este metodo, se ignore el filtro de IsDeleted y se pueden conseguir las entidades padres que estan eliminadas (para mostrar quien sabe que datos necesitabamos para renderizar)


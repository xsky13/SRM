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

One ticket per NON-MANUAL only payment.


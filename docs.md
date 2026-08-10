# Documentacion

## Estructura api

/Config 	    	-> archivos de configuracion

/Controllers 	    -> controladores

/Data          	    -> todo con relacion a la base de datos

&#x09;/Migrations   -> migraciones

/Services 		    -> interfaces de servicios

&#x09;/Impl 	    -> implementaciones de las interfaces

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


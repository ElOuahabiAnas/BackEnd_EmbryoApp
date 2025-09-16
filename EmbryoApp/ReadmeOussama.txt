il y'a deux api pour le registre , il y'a /identity/register (pas conseilé) et 
il y'a /auth/register (c'est celui que tu doit travailler avec )


l'api /auth/registre est dédié pour les etudiant afin qu'il puisse crée un compte 
dans l'application et se connecter avec le role "Student"


pour que tu puisse tester tous mes Api , tu doit connecter avec un user qui a le role 
"Professor"

pour cree un user avec le role professeur tu doit le crée dans la base de donnée manuelement 
dans la table "AspNetUserRoles"


1) Prérequis
.NET SDK 9.0
PostgreSQL 14+

2) Cors : ajoute ton origin front


ce qui manque dans mes api :
1/ un professeur doit avoir la possibilite de crée un autre professeur.

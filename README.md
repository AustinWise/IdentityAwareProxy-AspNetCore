
# Google Cloud Identity Aware Proxy authentication handler for ASP.NET Core

This is a work in progress. The goal is to create an ASP.NET authentication provider
for [Google Cloud Identity Aware Proxy](https://cloud.google.com/iap).

Currently implemented features:

* Uses the user's email as the username.
* Maps IAP access levels to roles.

NOTE: currently this codebase does not completely prevent misconfigured apps from
processing requests. Specifically JWTs for the wrong audience are accepted.
That is, someone could capture an IAP token and forward it to this app and the
request would keep processing.

The solution is to this problem is to somehow share the same settings between
the authentication handler and the middleware.

## TODO

* Check the IP address of the incoming connection if possible.
* Add an option to require all connections to be authenticated with IAP.
* Actually implement something interesting in the example app.
* Add an IAP simulator for testing.
* Add support [external identities](https://cloud.google.com/iap/docs/enable-external-identities).
* Figure out if there is a good way to pass the IAP JWT to a backend service.
  Probably by adding an option for the authentication phase to look for the JWT in a different header.
  This would allow a [service account to authenicate with the IAP](https://cloud.google.com/iap/docs/authentication-howto#authenticating_from_a_service_account),
  then the middle ware would validate the JWT, and then the authnetication phase would load the user's identity from
  the extra header. The backend service could either trust that the frontend validated the audience values or do its own validation.
* Add options for customizing how the username is chosen.
  * Email (current default)
  * User id, with or without the `accounts.google.com:` prefix.
  * A custom delegate on the events object.
  * See also [these docs](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/social/additional-claims?view=aspnetcore-7.0)
    for inspiration.
* Add options for customizing how IAP access levels are translated into roles.
  * Strip prefix
  * Custom delegate on the event object for transforming.


# Google Cloud Identity Aware Proxy authentication handler for ASP.NET Core

This is a work in progress. The goal is to create an ASP.NET authentication provider
for [Google Cloud Identity Aware Proxy](https://cloud.google.com/iap).

Currently implemented features:

* Blocks all connections that have a missing or invalid IAP JWT.
* Uses the user's email as the username.
* Maps IAP access levels to roles.

## TODO

* Check the IP address of the incoming connection if possible.
* Actually implement something interesting in the example app.
* Add an IAP simulator for testing.
* Add support [external identities](https://cloud.google.com/iap/docs/enable-external-identities).
* Add the option to validate the audience of the JWT in the alternate header.
* Add options for customizing how the username is chosen.
  * Email (current default)
  * User id, with or without the `accounts.google.com:` prefix.
  * A custom delegate on the events object.
  * See also [these docs](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/social/additional-claims?view=aspnetcore-7.0)
    for inspiration.
* Add options for customizing how IAP access levels are translated into roles.
  * Strip prefix
  * Custom delegate on the event object for transforming.
* Add NativeAOT / Trim compatability. This might require using a different library to validate the JWT.


# Google Cloud Identity Aware Proxy authentication handler for ASP.NET Core

This is a work in progress and not an official Google project.
This library is for some personal projects I'm working on.
The goal is to create an ASP.NET Core authentication provider
for [Google Cloud Identity Aware Proxy](https://cloud.google.com/iap).

Currently implemented features:

* Blocks all connections that have a missing or invalid IAP JWT.
* Sets the HttpContext.User to a principal that:
  * Uses the subject claim of the IAP JWT as a user name (it looks like "accounts.google.com:1234", where 1234 is the user's ID)
  * An email claim containing the user's email address.
  * Access levels are set as the roles for the user.
* A simulator GUI for simulating IAP when testing locally.

## TODO

* Actually implement something interesting in the example app.
* Consider integrating with
  [ASP.NET Identity](https://learn.microsoft.com/en-us/aspnet/identity/overview/getting-started/introduction-to-aspnet-identity).
  This might make it easier for the user identity to be part of a larger Entity Framework database schema.
  This might be challenging to make smooth, as ASP.NET Identity treats its cookie as the source of user identity
  and this is not currently customizable.
* Add support [external identities](https://cloud.google.com/iap/docs/enable-external-identities).
* Add the option to validate the audience of the JWT in the alternate header.
* Add options for customizing how the username is chosen.
  * User id, with the `accounts.google.com:` prefix. (current default)
  * Email
  * A custom delegate on the events object.
  * See also [these docs](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/social/additional-claims?view=aspnetcore-7.0)
    for inspiration.
* Add options for customizing how IAP access levels are translated into roles.
  * Disable translating to roles
  * Strip prefix
  * Custom delegate on the event object for transforming.
* Add NativeAOT / Trim compatibility. This might require using a different library to validate the JWT.
* Test on App Engine, including checking what IP address the requests come from and filtering out
  bad IP address in `IapMiddleware`.

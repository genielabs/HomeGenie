# HomeGenie Panel push notifications

Program 580 (`homegenie-panel-notifications`), version 1.0.1, supports removing a registered phone token:

```text
/api/HomeAutomation.HomeGenie/Config/Notifications.Unregister/<token>
```

Use the same server authentication as other API calls and URL-encode the token as a path segment. No model argument is required.

The handler removes all records matching that token from `panel_devices.json`, leaving other phones registered. It returns an OK response with message `Unregistered` even if the token is already absent. A missing or empty token returns an error with message `Token missing`.

Unregistering prevents subsequent sends to that token; it does not cancel requests already in flight. The Android app must stop automatic registration for an opted-out server before using this endpoint, or it could register the token again. The app subscription checkbox is a separate change.

# Part 3 video plan

Target length: 15 to 20 minutes, in your own voice, uploaded to YouTube as **Unlisted**.

Before recording:

- The VPS is up and `http://<vps-ip>:5000/swagger` opens from a phone on mobile data.
- The latest commit on `main` has a green run in the Actions tab.
- Close or hide anything that shows a secret: the `.env` file, `appsettings.json`, GitHub secret values, Docker Hub tokens, SSH keys. Show `.env.example` instead. If a token appears in Postman or Swagger, that is fine for a test account, but do not show the VPS `.env` or the Admin password.
- In Postman, select the **CoffeeNChill Part 3** environment, set `baseUrl` to the VPS URL, and fill in `adminEmail` / `adminPassword` before you start (off camera).

## 1. Introduction (1 min)

- Names, student numbers and what each person did (README "Who worked on what").
- One sentence on the system: clients call the ASP.NET Core gateway, which checks the JWT and talks to Azure Table Storage and the Functions app.

## 2. Live deployment (2 min)

- Open the live Swagger URL in a browser and point out it is the VPS IP, not localhost.
- Show the same URL working on a phone on mobile data (screen mirror or a short clip).
- On the VPS (SSH), run `docker compose ps` in `~/coffeenchill` to show the three containers running.
- Show `sudo ufw status` (only SSH and 5000 open) and the cloud firewall rule.
- Mention `restart: unless-stopped` and Docker starting on boot, so the app comes back after a reboot.

## 3. Auth: 401, 403 and a successful call (4 min)

Use Swagger or Postman against the live URL:

1. `GET /api/menu` with no token returns **200** (public).
2. `POST /api/menu` with no token returns **401**. Point out the JSON error body.
3. Register a Customer (`POST /api/auth/register`) and log in (`POST /api/auth/login`). Copy the token.
4. Swagger **Authorize** with the Customer token, then `POST /api/menu` returns **403**. Explain 401 vs 403.
5. Log in as the Admin, create a Staff account with `POST /api/auth/users`, log in as Staff, then `POST /api/menu` returns **201**.
6. Show public registration with `"role": "Admin"` returning **403**: only an Admin can create Staff or Admin.
7. Paste the token into <https://jwt.io> to show the name, email and role claims and the expiry. Do not paste the signing key.

## 4. Key code (4 min)

- `Program.cs`: JWT settings read from configuration, the middleware order, and Swagger with the Authorize button.
- `Security/RegistrationPolicy.cs`: the rule for who can create which role.
- `Services/PasswordHasher.cs`: BCrypt, and the Users table in Azurite showing only a hash (Azure Storage Explorer).
- `Dtos/AuthDtos.cs`: validation attributes. Show a 400 with the `errors` object.
- `Middleware/GlobalExceptionMiddleware.cs`: one error format.
- `CoffeeNChillGateway.Tests`: run `dotnet test` and show it passing.

## 5. Docker and Docker Hub (2 min)

- Gateway `Dockerfile` (multi-stage, non-root, port 8080).
- `docker-compose.yml`: three services on one network, images pulled from Docker Hub, settings from `.env`.
- The Docker Hub pages for `coffeenchill-gateway` and `coffeenchill-functions`, with their tags.

## 6. CI/CD pipeline (2 min)

- `.github/workflows/ci-cd.yml`: build and test, push to Docker Hub, deploy to the VPS.
- The Actions tab: a green run on `main` with all three jobs.
- Settings > Secrets: show the secret **names** only (the values cannot be viewed anyway).

## 7. Postman against the live URL (1 min)

- Run the whole collection with `baseUrl` set to the VPS, and show the summary with 0 failures.

## 8. Wrap up (30 s)

- What works, anything not finished, and where it is documented (README and `docs/`).

After uploading: open the link in a private browser window to check it plays, then put it at the top of the README.

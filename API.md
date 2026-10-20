# Paintings API

The ASP.NET API stores image files in Supabase Storage and painting metadata in the `public.paintings` table in Supabase PostgreSQL.

## Configuration

Set these server-side environment variables (or add them to the development `.env` file):

| Variable | Purpose |
| --- | --- |
| `SUPABASE_URL` | Supabase project URL |
| `SUPABASE_SECRET_KEY` | Server-only Supabase secret key; never send it to a browser |
| `SUPABASE_STORAGE_BUCKET` | Storage bucket name; defaults to `paintings` |
| `PAINTINGS_API_KEY` | Separate key required by upload and delete API endpoints |
| `ConnectionStrings__PaintingsDb` | PostgreSQL connection string copied from the Supabase dashboard |

In Supabase, open **Connect** and select a connection method. The app accepts the `postgresql://...` URL Supabase provides, or an Npgsql keyword/value connection string. For a long-running ASP.NET host, Supabase recommends the direct connection when the host supports IPv6; use the session pooler for IPv4-only hosts. Keep the database password in this environment variable, not in `appsettings.json`.

Example format (replace every placeholder with values from **Connect**):

```text
ConnectionStrings__PaintingsDb=Host=<host>;Port=<port>;Database=postgres;Username=<username>;Password=<database-password>;SSL Mode=Require
```

The EF model maps to `public.paintings` with columns `id`, `title`, `medium`, `width`, `height`, `image_path`, and `uploaded_at`. `id` must be `bigint`; `width` and `height` should be `numeric(8,2)`.

Create the bucket in Supabase Storage and make it public so gallery image URLs can be loaded by visitors. Keep `SUPABASE_SECRET_KEY` and `PAINTINGS_API_KEY` out of source control and client-side code.

## Endpoints

### `GET /api/paintings`

Returns paintings ordered by upload time, newest first. This endpoint is public.

### `POST /api/paintings`

Uploads an image and creates its painting record. Send `multipart/form-data` with `title`, `medium`, `width`, `height`, and `imageFile`. Include the `X-Paintings-Api-Key` header. Images must be JPEG, PNG, WebP, or GIF and no larger than 20 MB.

### `DELETE /api/paintings/{id}`

Deletes the image from Supabase Storage and removes its painting record. Include the `X-Paintings-Api-Key` header.

The upload and delete endpoints are intended for trusted server-side callers. Do not put the API key in browser JavaScript.

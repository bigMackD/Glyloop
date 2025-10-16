1. Tables with Columns, Data Types, and Constraints

## Enum Types
```sql
CREATE TYPE event_type AS ENUM ('food','insulin','exercise','note');
CREATE TYPE source_type AS ENUM ('manual','dexcom');
CREATE TYPE absorption_hint AS ENUM ('rapid', 'normal', 'slow', 'other');
CREATE TYPE intensity_type AS ENUM ('light','moderate','vigorous');
CREATE TYPE insulin_type AS ENUM ('fast','long');
```

## Tables

### events

| Column Name | Data Type | Constraints | Description |
|-------------|-----------|-------------|-------------|
| id | UUID | PRIMARY KEY DEFAULT gen_random_uuid() | Unique event identifier |
| user_id | UUID | NOT NULL REFERENCES aspnetusers(id) ON DELETE CASCADE | Owning user |
| created_at | TIMESTAMPTZ | NOT NULL DEFAULT now() | Record creation timestamp |
| event_time | TIMESTAMPTZ | NOT NULL | Time when event occurred |
| type | event_type | NOT NULL | Event type enum |
| source | source_type | NOT NULL | Origin of event |


### event_food

| Column Name | Data Type | Constraints | Description |
|-------------|-----------|-------------|-------------|
| id | UUID | PRIMARY KEY REFERENCES events(id) ON DELETE CASCADE | Reference to base event |
| carbs_g | INTEGER | NOT NULL CHECK (carbs_g BETWEEN 0 AND 300) | Amount of carbohydrates in grams |
| meal_tag_id | INTEGER | NOT NULL REFERENCES meal_tags(id) | Tag for meal categorization |
| absorption_hint | absorption_hint | NOT NULL | Meal absorption speed |
| note | VARCHAR(500) |  | Optional note |



### event_insulin

| Column Name     | Data Type      | Constraints                                                        | Description                          |
|-----------------|----------------|--------------------------------------------------------------------|--------------------------------------|
| id              | UUID           | PRIMARY KEY REFERENCES events(id) ON DELETE CASCADE                | Reference to base event              |
| insulin_units   | NUMERIC(5,2)   | NOT NULL CHECK (insulin_units BETWEEN 0 AND 100 AND (insulin_units * 2) % 1 = 0) | Insulin dose in units                |
| insulin_type    | insulin_type   | NOT NULL                                                           | Fast-acting or long-acting insulin   |
| preparation     | TEXT           |                                                                    | Insulin preparation type             |
| delivery        | TEXT           |                                                                    | Delivery method                      |
| timing          | TEXT           |                                                                    | Timing relative to meal              |
| note            | TEXT           |                                                                    | Optional note                        |



### event_exercise

| Column Name | Data Type | Constraints | Description |
|-------------|-----------|-------------|-------------|
| id | UUID | PRIMARY KEY REFERENCES events(id) ON DELETE CASCADE | Reference to base event |
| duration_min | INTEGER | NOT NULL CHECK (duration_min BETWEEN 1 AND 300) | Duration in minutes |
| exercise_type_id | INTEGER | NOT NULL REFERENCES exercise_types(id) | Type of exercise |
| intensity | intensity_type | NOT NULL | Exercise intensity level |



### event_note

| Column Name | Data Type | Constraints | Description |
|-------------|-----------|-------------|-------------|
| id | UUID | PRIMARY KEY REFERENCES events(id) ON DELETE CASCADE | Reference to base event |
| note_text | VARCHAR(500) | NOT NULL | Note content |


### meal_tags

| Column Name | Data Type | Constraints | Description |
|-------------|-----------|-------------|-------------|
| id | SERIAL | PRIMARY KEY | Unique tag identifier |
| label | TEXT | NOT NULL UNIQUE | Tag label |
| created_at | TIMESTAMPTZ | NOT NULL DEFAULT now() | Creation timestamp |


### exercise_types

| Column Name | Data Type | Constraints | Description |
|-------------|-----------|-------------|-------------|
| id | SERIAL | PRIMARY KEY | Unique exercise type identifier |
| label | TEXT | NOT NULL UNIQUE | Type label |
| created_at | TIMESTAMPTZ | NOT NULL DEFAULT now() | Creation timestamp |


### dexcom_links

| Column Name | Data Type | Constraints | Description |
|-------------|-----------|-------------|-------------|
| id | UUID | PRIMARY KEY DEFAULT gen_random_uuid() | Link identifier |
| user_id | UUID | NOT NULL REFERENCES aspnetusers(id) ON DELETE CASCADE | Owning user |
| access_token_encrypted | BYTEA | NOT NULL | Encrypted access token |
| refresh_token_encrypted | BYTEA | NOT NULL | Encrypted refresh token |
| token_expires_at | TIMESTAMPTZ | NOT NULL | Token expiry datetime |
| last_refreshed_at | TIMESTAMPTZ | NOT NULL | Last refresh timestamp |


### user_preferences

| Column Name | Data Type | Constraints | Description |
|-------------|-----------|-------------|-------------|
| user_id | UUID | PRIMARY KEY REFERENCES aspnetusers(id) ON DELETE CASCADE | Owning user |
| tir_lower | SMALLINT | NOT NULL DEFAULT 70 | Lower bound of TIR target |
| tir_upper | SMALLINT | NOT NULL DEFAULT 180 | Upper bound of TIR target |
| CHECK (tir_lower < tir_upper) |  |  | Ensure lower is less than upper |


2. Relationships Between Tables
- `events.user_id` → `aspnetusers(id)` (one-to-many)
- One-to-one TPT inheritance: each `event_*` table’s `id` → `events(id)`
- `event_food.meal_tag_id` → `meal_tags(id)`
- `event_exercise.exercise_type_id` → `exercise_types(id)`
- `dexcom_links.user_id` → `aspnetusers(id)` (one-to-many)
- `user_preferences.user_id` → `aspnetusers(id)` (one-to-one)

3. Indexes
```sql
-- Composite index for event history filtering
CREATE INDEX idx_events_user_time_type
  ON events(user_id, created_at, type);

-- Detail-table indexes for type-specific queries
CREATE INDEX idx_event_food_carbs
  ON event_food(carbs_g);
CREATE INDEX idx_event_insulin_units
  ON event_insulin(insulin_units);
CREATE INDEX idx_event_exercise_duration
  ON event_exercise(duration_min);
```

4. PostgreSQL Policies
- No Row-Level Security (RLS) for MVP.

5. Additional Notes
- CGM readings and +2h outcomes are not persisted; handled client-side via Dexcom API.
- All timestamp columns use `TIMESTAMPTZ NOT NULL DEFAULT now()` for UTC consistency.
- Sensitive tokens in `dexcom_links` encrypted via `pgcrypto` with a key stored in environment/KMS.
- No soft deletes, audit logs, partitioning, or triggers are implemented for MVP simplicity.


CREATE TABLE users
(
    id uuid PRIMARY KEY,
    email varchar(254) NOT NULL,
    normalized_email varchar(254) NOT NULL,
    password_hash text NOT NULL,
    CONSTRAINT uq_users_normalized_email UNIQUE (normalized_email)
);

CREATE TABLE tasks
(
    id uuid PRIMARY KEY,
    owner_id uuid NOT NULL,
    title varchar(200) NOT NULL,
    description varchar(2000),
    status varchar(16) NOT NULL,
    due_date date,
    CONSTRAINT fk_tasks_owner FOREIGN KEY (owner_id) REFERENCES users(id),
    CONSTRAINT ck_tasks_title_not_blank CHECK (char_length(btrim(title)) > 0),
    CONSTRAINT ck_tasks_status CHECK (status IN ('Pending', 'Wip', 'Completed'))
);

CREATE INDEX idx_tasks_owner_sort ON tasks(owner_id, status, due_date, id);

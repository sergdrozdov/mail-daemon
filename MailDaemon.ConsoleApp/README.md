Description
-------------------------------------------------
Create mail sending utility with configurable settings.


Arguments
-------------------------------------------------
-v      - validation mode.
-d      - send demo mail to sender.
-h      - help information.


Folders
-------------------------------------------------
MailProfiles – folder to store JSON mail profiles.
MailTemplates – folder to store mail templates.


Mail profile
-------------------------------------------------
mailProfile_Default.json
{
    "sender":
    {
        "address": "<mail address>",
        "name": "<name>"
    },
    "subject": "<subject>",
    "template": "<path to mail template>",
    "attachments": [
        {
            "path": "<path to file>",
            "filename": "<file name>"
        }
    ],
    "recipients": [
        {
            "address": "<mail address>",
            "name": "<name>",
            "subject": "<subject override parent>",
            "company": "<company>"
            "template": "<path to mail template override parent>"
            "attachments": [
                {
                    "path": "<path to file>",
                    "filename": "<file name>"
                }
            ],
        }
    ]
}


Text placeholders
-------------------------------------------------
{PERSON_NAME} – recipient name.
{COMPANY_NAME} – recipient company name.


Validation mode
-------------------------------------------------
Run Mail Daemon with attribute -v to validate JSON mail profile: no any mail sending, just validation.
mail-daemon.exe -v

Run Mail Daemon with attributes -v -d to validate JSON mail profile: no any recipient mail sending, just validation and sending demo mail to sender address.
mail-daemon.exe -v -d
Description
-------------------------------------------------
Batch mail sending utility with configurable settings.


Arguments
-------------------------------------------------
A console application can be launched with various arguments.
| Argument  | Description
| --------- | ------------
| -v        | validation mode.
| -d        | send demo mail to sender.
| -p        | profile file name. E.g.: -p subscribers.json
| -gp       | create files on disk with generated mails for each recipient.
| -h        | help information.


Folders
-------------------------------------------------
MailProfiles – folder to store JSON mail profiles.  
MailTemplates – folder to store mail templates.


Mail profile
-------------------------------------------------
| Value         | Description
| ------------- | ------------
| **sender**    | **address** is required value.
| **subject**   | is required value.
| **template**  | is required value
| **recipients**| **address** is required value.
```
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
            "template": "<path to mail template override parent>",
			"replace": {
				"custom-text": "<replace with>",
                "url": "<replace with>"
			},
            "attachments": [
                {
                    "path": "<path to file>",
                    "filename": "<file name>"
                }
            ],
        }
    ],
    "replace":
    {
        "text1": "replace text",
        "text2": "replace text"
    }}
```

Text placeholders
-------------------------------------------------
\{PERSON_NAME\} – recipient name.  
\{COMPANY_NAME\} – recipient company name.


Validation and demo mode
-------------------------------------------------
Run Mail Daemon with attribute -v to validate JSON mail profile: no any mail sending, just validation.\
mail-daemon **-v**

Run Mail Daemon with attributes -v -d to validate JSON mail profile: no any recipient mail sending, just validation and sending demo mail to sender address.\
mail-daemon **-v -d**

Run Mail Daemon with creation of generated mail files.\
mail-daemon **-gp**

Run Mail Daemon with custom mail profile.\
mail-daemon **-p custom-profile.json**
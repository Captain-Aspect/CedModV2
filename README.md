CedMod builds are located on the Releases tab Please note that there are always 2 releases for each version, one for the NWApi and one for EXILED. Please take note of this when installing.
The CedMod instance creation guide https://cedmod.nl/Servers/Create will also provide the 2 releases after creating an instance.
if you already have an instance the "View Setup command" or "Add new QueryServer" button will also provide you with the releases in question.

About CedMod: https://cedmod.nl/About

Setup guide located here: https://cedmod.nl/Servers/Setup

Support discord: https://discord.gg/p69SGfwxxm


| config                              | type      | default  | description                                                                |
|-------------------------------------|----------:|:--------:|:--------------------------------------------------------------------------:|
| cm: MainPlugin                                                                                                                          |
| is_enabled                          |   bool    | true     | If the plugin is enabled                                                   |
| kick_same_name                      |   bool    | true     | If Users with the same name will be kick                                   |
| ced_mod_api_api                     |   string  | none     | API Key for the CedMod API                                                 |
| autoban_enabled                     |   bool    | false    | if the FF autoban is enabled                                               |
| autoban_threshold                   |   int     | 3        | the amount of Teamkills before the autoban will ban                        |
| autoban_duration                    |   int     | 4320     | the amount of time a user will be banned after triggering the autoban      |
| autoban_reason                      |   string  | You have teamkilled too many people | the reason for the autoban bans                 |
| autoban_disarmed_class_d_tk         |   bool    | true     | if disarmed class D kills are considered teamkills                         | 
| autoban_disarmed_scientist_d_tk     |   bool    | true     | if disarmed scientist kills are considered teamkills                       |
| autoban_class_dvs_class_d           |   bool    | true     | if class D vs class D kills are considered teamkills                       |
| report_blacklist                    |   list    | []       | userids in this list will not be able to use ingame reports                |
| staff_report_allowed                |   bool    | false    | if staff can be reported using ingame reports                              |
| staff_report_message                |   string  | You can not report a staff member | Message shown when staff reporting is not allowed |
| additional_ban_message              |   string  | ""       | String appended to the ban kick message                                    |
| show_debug                          |   bool    | false    | If debug messages are shown, Warning: This WILL spam your console          |
|                                                                                                                                         |
| cm_events                                                                                                                               |
| is_enabled                          |   bool    | true     | If the plugin is enabled                                                   |
| debug                               |   bool    | false    | If debug messages are shown, Warning: This WILL spam your console          |
|                                                                                                                                         |
| cm_WAPI                                                                                                                                 |
| is_enabled                          |   bool    | true     | If the plugin is enabled                                                   |
| disallowed_web_commands             |   list    | []       | Commands that will not be able to run                                      |
| security_key                        |   string  | string   | key for remote commands, set the same as in the panel                      |
| identifier                          |   string  | "Server 1"| The name that your server will appear as on the panel                     |
| enable_ban_reason_sync              |   bool    | true     | If the querysystem should sync ban predefined ban reasons with the panel   |
| enable_external_lookup              |   bool    | true     | If External Lookup should be enabled automatically                         |
| custom_sever_full_message           |   string  | ""       | The message shown to users once the server is full, leave empty for game default |
| debug                               |   bool    | false    | If debug messages are shown, Warning: This WILL spam your console           |

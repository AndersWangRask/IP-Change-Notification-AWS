# IP Change Notification (AWS)
--------

The IP Change Notification is a small util that can be scheduled to run periodically and on system start-up to detect if the External IP address has changed. With some ISP the External IP will change quite frequently, but the External IP will also change when a laptop moves from one location to another.

When the External IP address has changed the util can be configured do the following 

* Update rules for AWS EC2 Security Group
* Update AWS Route 53 record for host
* Update a central encrypted record in AWS Route 53 with all the users of the util's IP address
* Send notifications via e-mail to a list of configured recipients that the External IP address has been changed

--------

Using this util can be helpful for e.g. Remote Workers who manage or otherwise access AWS infrastructure.

I have adapted this util from an old script we used for a Remote Team I was part of.

I have tried to make the architecture sort-of-like plugin based and it would be possible to develop additional plugins e.g. targeting non-AWS.

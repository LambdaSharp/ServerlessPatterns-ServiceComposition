# Service Composition

This repository shows how services can be composed by importing values from deployed CloudFormation stacks.

## BitcoinTopic

The _BitcoinTopic_ module creates a Lambda function that publishes the most recent bitcoin price on an SNS topic. The SNS topic is exported from the CloudFormation stack so that other stacks can subscribe to it.

## BitcoinTable

The _BitcoinTable_ module creates a Lambda function that subscribes to the SNS topic from the _BitcoinTopic_ stack and stores the received value in a DynamoDB table. Stored values are automatically forgotten after 15 minutes to minimize the number of stored rows. The DynamoDB table is exported for other stacks to query against.

## BitcoinActivity

The _BitcoinActivity_ module creates a REST API for recording buy/sell activity. It imports the table from the _BitcoinTable_ stack to fetch the most recently recorded price.

A Postman collection is provided to easily interact with the REST API.

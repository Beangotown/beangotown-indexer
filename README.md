# beangotown-indexer
BRANCH | AZURE PIPELINES                                                                                                                                                                                                                             | TESTS                                                                                                                                                                                                  | CODE COVERAGE
-------|---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|--------------
MASTER   | [![Build Status](https://dev.azure.com/BeangoTown/beangotown-indexer/_apis/build/status/beangotown-indexer?branchName=master)](https://dev.azure.com/BeangoTown/beangotown-indexer/_build/latest?definitionId=3&branchName=master) | [![Test Status](https://img.shields.io/azure-devops/tests/BeangoTown/beangotown-indexer/4/master)](https://dev.azure.com/BeangoTown/beangotown-indexer/_build/latest?definitionId=4&branchName=master) | [![codecov](https://codecov.io/github/Beangotown/beangotown-indexer/graph/badge.svg?token=V2WBZENI8E)](https://codecov.io/github/Beangotown/beangotown-indexer)

## Introduction
BeangoTown Indexer is an interface plugin project running on the aelf blockchain scanning system, acting as a bridge between blockchain data and Web3 applications. In simple terms, its main functions include:

- Processing block data delivered by the aelf scanning system:
    - The BeangoTown Indexer plugin runs on the aelf scanning system, obtaining and processing on-chain block data in real-time. This data includes transaction information, contract calls, etc., which can be filtered and organized according to the  requirements of the BeangoTown.
- Recording the processed results to ElasticSearch:
    - To achieve efficient data retrieval and querying, BeangoTown Indexer needs to aggregate and store the processed blockchain data in ElasticSearch. ElasticSearch is a high-performance full-text search engine capable of quickly querying large amounts of data in real-time.
- Providing real-time data access API using GraphQL specification:
    - To facilitate third-party applications (such as Web3 Apps) to access the processed blockchain data, BeangoTown Indexer provides an API that follows the GraphQL specification. GraphQL is a data query and manipulation language that allows clients to obtain the required data more flexibly and improve data transmission efficiency.

Therefore, based on the design of the aelf scanning system, any Dapp that wants to obtain aelf on-chain business information needs a scanning interface plugin similar to BeangoTown Indexer. Such a plugin can not only provide real-time on-chain data for Dapps but also optimize data query and access performance, providing convenience for developers to build efficient blockchain applications.

## Test

You can easily run unit tests on BeangoTown Indexer. Navigate to the BeangoTown.Indexer.Tests run:

```Bash
cd test/BeangoTown.Indexer.Tests
dotnet test
```

## Usage

The BeangoTown Indexer provides the following modules:

- `Entities`: Define the index structure used for ElasticSearch.
- `GraphQL`: Provide GraphQL APIs and related definitions of input and output.
- `Processors`: Process and store block data from aelf blockchain scanning system.

## Contributing

We welcome contributions to the BeangoTown Indexer project. If you would like to contribute, please fork the repository and submit a pull request with your changes. Before submitting a pull request, please ensure that your code is well-tested and adheres to the aelf coding standards.
## License

BeangoTown Indexer is licensed under [MIT](https://github.com/Beangotown/beangotown-indexer/blob/master/LICENSE).


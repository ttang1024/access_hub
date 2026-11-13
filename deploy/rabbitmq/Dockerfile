# RabbitMQ with the delayed message exchange plugin, used by MassTransit's delayed message scheduler
# (alarm escalation). Plugin 4.2.x supports RabbitMQ 4.2.x.
FROM rabbitmq:4.2-management

ARG DELAYED_EXCHANGE_VERSION=4.2.0
ADD https://github.com/rabbitmq/rabbitmq-delayed-message-exchange/releases/download/v${DELAYED_EXCHANGE_VERSION}/rabbitmq_delayed_message_exchange-${DELAYED_EXCHANGE_VERSION}.ez /opt/rabbitmq/plugins/

# Files added from a URL are mode 600; the broker runs as the rabbitmq user.
RUN chmod 644 /opt/rabbitmq/plugins/rabbitmq_delayed_message_exchange-${DELAYED_EXCHANGE_VERSION}.ez \
    && rabbitmq-plugins enable --offline rabbitmq_delayed_message_exchange

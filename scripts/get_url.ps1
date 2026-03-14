$env:AWS_PAGER = ""
aws cloudformation describe-stacks `
    --stack-name "RequiemNexus-Compute-Stack" `
    --query "Stacks[0].Outputs[?OutputKey=='LoadBalancerUrl'].OutputValue" `
    --output text

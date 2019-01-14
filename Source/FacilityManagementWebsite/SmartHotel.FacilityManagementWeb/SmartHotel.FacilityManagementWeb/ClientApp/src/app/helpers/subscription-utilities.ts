import { Subscription } from 'rxjs';

export class SubscriptionUtilities {
    public static tryUnsubscribe(subscriptionToUnsubscribe: Subscription) {
        if (subscriptionToUnsubscribe) {
            subscriptionToUnsubscribe.unsubscribe();
        }
    }
}
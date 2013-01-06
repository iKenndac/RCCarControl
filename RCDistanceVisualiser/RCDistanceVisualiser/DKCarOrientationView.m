//
//  DKCarOrientationView.m
//  RCDistanceVisualiser
//
//  Created by Daniel Kennett on 06/01/2013.
//  Copyright (c) 2013 Daniel Kennett. All rights reserved.
//

#import "DKCarOrientationView.h"

@implementation DKCarOrientationView

- (id)initWithFrame:(NSRect)frame
{
    self = [super initWithFrame:frame];
    if (self) {
        // Initialization code here.
    }
    
    return self;
}

-(void)drawRect:(NSRect)dirtyRect {
	NSRect bounds = self.bounds;
    NSPoint centerPoint = NSMakePoint(NSMidX(bounds), NSMidY(bounds));

	// Z is up-down (positive when the car is right-way up)
	// Y is front-back (acceleration and braking)

	// http://stackoverflow.com/questions/7522436/ios-accelerometer-gyroscope-question

	[[NSBezierPath bezierPathWithOvalInRect:(NSRect){centerPoint, NSMakeSize(4.0, 4.0)}] fill];

	double zValue = self.z * 50.0;
	double yValue = self.y * 50.0;

	[[NSBezierPath bezierPathWithOvalInRect:(NSRect){NSMakePoint(centerPoint.x - yValue, centerPoint.y - zValue), NSMakeSize(4.0, 4.0)}] fill];
	

}

@end
